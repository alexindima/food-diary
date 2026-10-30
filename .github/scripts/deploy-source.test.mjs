import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { mkdtempSync, readFileSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";
import { test } from "node:test";
import { images, createMatrix, createReceipt, collectReceipts } from "./deploy-images.mjs";

const workflow = await readFile(
    new URL("../workflows/deploy.yml", import.meta.url),
    "utf8",
);
const expression = workflow
    .match(/    if: >-\r?\n([\s\S]*?)    runs-on:/)?.[1]
    .trim();
assert.ok(expression, "Deploy must authorize the event at job level");
const allows = new Function("github", `return (${expression});`);
const repository = "alexindima/food-diary";
const push = {
    conclusion: "success",
    event: "push",
    head_branch: "master",
    head_repository: { full_name: repository },
};

for (const [name, event, ref, run, expected] of [
    ["master push", "workflow_run", "refs/heads/master", push, true],
    [
        "fork PR named master",
        "workflow_run",
        "refs/heads/master",
        {
            ...push,
            event: "pull_request",
            head_repository: { full_name: "attacker/fork" },
        },
        false,
    ],
    [
        "same repository PR",
        "workflow_run",
        "refs/heads/master",
        { ...push, event: "pull_request" },
        false,
    ],
    [
        "foreign push",
        "workflow_run",
        "refs/heads/master",
        { ...push, head_repository: { full_name: "attacker/fork" } },
        false,
    ],
    [
        "other branch",
        "workflow_run",
        "refs/heads/master",
        { ...push, head_branch: "feature" },
        false,
    ],
    [
        "failed CI",
        "workflow_run",
        "refs/heads/master",
        { ...push, conclusion: "failure" },
        false,
    ],
    [
        "manual master",
        "workflow_dispatch",
        "refs/heads/master",
        undefined,
        true,
    ],
    [
        "manual feature",
        "workflow_dispatch",
        "refs/heads/feature",
        undefined,
        false,
    ],
]) {
    test(name, () =>
        assert.equal(
            allows({
                repository,
                event_name: event,
                ref,
                event: { workflow_run: run },
            }),
            expected,
        ),
    );
}

test("source authorization runs before checkout, credentials and source scripts", () => {
    const guard = workflow.indexOf("- name: Authorize deployment source");
    assert.ok(guard > 0 && guard < workflow.indexOf("- name: Checkout code"));
    assert.ok(
        workflow.includes(
            '"${GITHUB_API_URL}/repos/${GITHUB_REPOSITORY}/git/ref/heads/master"',
        ),
    );
    assert.ok(workflow.includes('"${SOURCE_COMMIT_SHA}" != "${master_sha}"'));
    assert.ok(workflow.includes("curl --fail --silent --show-error"));
});

test("database readiness runs before server uploads and writer shutdown", () => {
    const guard = workflow.indexOf("- name: Verify primary database baseline");
    const deployment = workflow.indexOf("- name: Deploy to server");
    assert.ok(guard > 0 && guard < deployment);
    const guardStep = workflow.slice(guard, workflow.indexOf("- name: Record failure step", guard));
    assert.ok(guardStep.includes("'cd /opt/fooddiary && python3 -' < scripts/database/check_primary_baseline_readiness.py"));
    assert.ok(!guardStep.includes("continue-on-error"));
    assert.ok(!guardStep.includes(" stop "));
    assert.ok(deployment < workflow.indexOf("$SCP_CMD docker-compose.yml"));
    assert.ok(deployment < workflow.indexOf("stop --timeout 60 api telegram-bot job-manager"));
});

test("server bounds independent chains and preserves the primary and publication barriers", () => {
    const remote = workflow.split("<< 'SCRIPT_EOF'\n")[1].split("          SCRIPT_EOF")[0];
    assert.ok(workflow.indexOf("$SCP_CMD scripts/deploy/parallel_tasks.sh") < workflow.indexOf("bash /tmp/deploy.sh"));
    assert.match(remote, /bash scripts\/deploy\/parallel_tasks\.sh "\$task_logs" 2/u);
    assert.match(remote, /deploy_tech_radar deploy_mailrelay deploy_mailinbox deploy_bugtriage 8>&- &/u);
    assert.match(remote, /trap finish_deployment EXIT/u);
    assert.equal([...remote.matchAll(/trap - EXIT/gu)].length, 1);
    assert.match(remote, /flock "\$FOODDIARY_INITIALIZER_LOCK" bash -e -c/u);
    assert.match(remote, /test -f "\$FOODDIARY_MAIN_MIGRATED"; exec "\$@"/u);
    const primary = remote.slice(remote.indexOf("backup_dir=/var/backups/fooddiary"));
    const positions = [
        "stop --timeout 60 api telegram-bot job-manager", "pg_dump --format=custom", "pg_restore --list",
        "docker compose --profile backend run -T --rm db-init update", ': > "$FOODDIARY_MAIN_MIGRATED"',
        "exec 8>&-", "--no-deps api", 'wait "$auxiliary_pid"', 'publish_frontend.py", "publish"',
        "--no-deps client-ssr", "--no-deps nginx",
    ].map(value => primary.indexOf(value));
    assert.ok(positions.every(value => value >= 0));
    assert.deepEqual(positions, [...positions].sort((left, right) => left - right));
});

test("parallel deployment behavioral tests are mandatory on the Linux CI runner", async () => {
    const ci = await readFile(new URL("../workflows/ci-tests.yml", import.meta.url), "utf8");
    const focused = ci.split("  llm-wiki-focused:\n")[1].split(/^  [\w-]+:/mu)[0];
    assert.match(focused, /runs-on: ubuntu-24\.04/u);
    assert.match(focused, /python3 -B -m unittest discover -s scripts\/deploy -p 'test_parallel_tasks\.py'/u);
    const step = focused.split("- name: Test bounded parallel server deployment and failure barriers")[1].split("- name:")[0];
    assert.doesNotMatch(step, /continue-on-error|if:/u);
});

const identity = { sourceSha: "a".repeat(40), runId: "1234", runAttempt: "2" };
const receipts = () => images.map(({ name }, index) => createReceipt({
    name, digest: `sha256:${(index + 1).toString(16).padStart(64, "0")}`, ...identity,
}));
const helper = fileURLToPath(new URL("./deploy-images.mjs", import.meta.url));
const job = name => workflow.match(new RegExp(`^  ${name}:\\r?\\n([\\s\\S]*?)(?=^  [\\w-]+:\\r?$|(?![\\s\\S]))`, "mu"))?.[1];

test("cache failure recovery permits one classified clean rebuild before signing", () => {
    const workers = job("build-images");
    assert.ok(workers.includes("id: cached_build"), "A failed production build must be classified before a single cache-free retry");
    const step = name => workers.split(`- name: ${name}\n`)[1]?.split("\n      - name:")[0];
    const cached = step("Build and push production image");
    const classify = step("Classify failed production build");
    const builder = step("Set up clean retry builder");
    const retry = step("Retry production image without cache");
    const select = step("Select successful image digest");
    assert.match(cached, /continue-on-error: true/u);
    assert.match(cached, /builder: \$\{\{ steps\.cached_builder\.outputs\.name \}\}/u);
    assert.match(cached, /cache-to: type=gha,scope=production-\$\{\{ matrix\.image \}\},mode=max,ignore-error=true/u);
    assert.match(classify, /if: \$\{\{ !cancelled\(\) && steps\.cached_build\.outcome == 'failure' \}\}/u);
    assert.match(classify, /node \.github\/scripts\/deploy-build-retry\.mjs classify/u);
    for (const branch of [builder, retry]) assert.match(branch, /if: steps\.cache_failure\.outputs\.retry == 'true'/u);
    assert.match(retry, /builder: \$\{\{ steps\.retry_builder\.outputs\.name \}\}/u);
    assert.match(retry, /no-cache: true/u);
    assert.doesNotMatch(retry, /cache-from:|cache-to:/u);
    for (const branch of [classify, builder, retry, select]) assert.doesNotMatch(branch, /continue-on-error:/u);
    for (const input of ["context", "file", "push", "provenance", "sbom", "tags", "build-args"]) {
        const value = source => source.match(new RegExp(`^          ${input}: (.*)(?:\\n((?:            .*\\n)*))?`, "mu"))?.[0];
        assert.ok(value(cached), input);
        assert.equal(value(retry), value(cached), input);
    }
    assert.equal([...workers.matchAll(/uses: docker\/build-push-action@/gu)].length, 2);
    assert.match(select, /id: build/u);
    assert.match(select, /node \.github\/scripts\/deploy-build-retry\.mjs select/u);
    for (const [name, value] of Object.entries({
        PRIMARY_OUTCOME: "cached_build.outcome", PRIMARY_DIGEST: "cached_build.outputs.digest",
        RETRY_OUTCOME: "retry_build.outcome", RETRY_DIGEST: "retry_build.outputs.digest",
    })) assert.ok(select.includes(name + ": " + "${{ steps." + value + " }}"), name);
    assert.ok(workers.indexOf("- name: Select successful image digest") < workers.indexOf("- name: Install Cosign"));
});

const missingCacheMessage = "failed to copy: GET https://productionresultssa32.blob.core.windows.net/actions-cache/fixture?sig=private-signed-value\nERROR CODE: BlobNotFound";
const failedBuild = message => ({ Status: "failed", Error: { Code: 2, Message: message } });
const recovery = () => import("./deploy-build-retry.mjs");

test("cache classifier recognizes the observed BuildKit cache blob failure", async () => {
    const { isMissingGitHubCacheBlob } = await recovery();
    assert.equal(isMissingGitHubCacheBlob(failedBuild(missingCacheMessage)), true);
    assert.equal(isMissingGitHubCacheBlob(failedBuild("failed to solve: " + missingCacheMessage)), true);
});

for (const [name, record] of [
    ["compile failure", failedBuild('process "dotnet publish" did not complete successfully: exit code: 1')],
    ["registry failure", failedBuild("failed to push ghcr.io/api: unauthorized")],
    ["registry blob missing", failedBuild(missingCacheMessage.replace("productionresultssa32.blob.core.windows.net/actions-cache", "ghcr.io/v2/api/blobs"))],
    ["unrelated Azure blob", failedBuild(missingCacheMessage.replace("/actions-cache/", "/user-uploads/"))],
    ["untrusted hostname", failedBuild(missingCacheMessage.replace(".net/", ".net.example.com/"))],
    ["cache authorization failure", failedBuild(missingCacheMessage.replace("BlobNotFound", "AuthorizationFailure"))],
    ["cache words in a failed RUN command", failedBuild(`process "echo ${missingCacheMessage}" did not complete successfully: exit code: 1`)],
    ["cancelled build", { ...failedBuild(missingCacheMessage), Status: "canceled" }],
    ["successful record", { ...failedBuild(missingCacheMessage), Status: "completed" }],
    ["missing history fields", {}],
]) {
    test(`cache classifier refuses automatic retry for ${name}`, async () => {
        assert.equal((await recovery()).isMissingGitHubCacheBlob(record), false);
    });
}

test("cache inspection reads only the matrix job builder with a bounded diagnostic command", async () => {
    const { inspectFailedBuild } = await recovery();
    const expected = failedBuild(missingCacheMessage);
    const actual = inspectFailedBuild("builder-job-1", (command, args, options) => {
        assert.equal(command, "docker");
        assert.deepEqual(args, ["buildx", "history", "inspect", "--builder", "builder-job-1", "--format", "json"]);
        assert.equal(options.timeout, 30_000);
        assert.equal(options.maxBuffer, 4 * 1024 * 1024);
        assert.deepEqual(options.stdio, ["ignore", "pipe", "pipe"]);
        return { status: 0, stdout: JSON.stringify(expected) };
    });
    assert.deepEqual(actual, expected);
});

test("unavailable or invalid Buildx diagnostics fail closed without exposing signed URLs", async () => {
    const { inspectFailedBuild } = await recovery();
    for (const result of [{ status: 1, stderr: missingCacheMessage }, { status: null, error: new Error(missingCacheMessage) },
        { status: 0, stdout: missingCacheMessage }]) {
        assert.throws(() => inspectFailedBuild("builder-job-1", () => result), error =>
            error.message.includes("automatic retry refused") && !error.message.includes("private-signed-value"));
    }
    for (const builder of [undefined, "", "--injected", "builder; echo secret"]) {
        assert.throws(() => inspectFailedBuild(builder, () => assert.fail("Invalid builders must not start Docker")), /valid isolated build builder/u);
    }
});

test("successful build selection uses the original digest or exclusively the successful retry", async () => {
    const { selectBuildDigest } = await recovery();
    const original = "sha256:" + "1".repeat(64);
    const retried = "sha256:" + "2".repeat(64);
    assert.equal(selectBuildDigest("success", original, "skipped", ""), original);
    assert.equal(selectBuildDigest("failure", original, "success", retried), retried);
    for (const [primary, retry] of [["failure", "failure"], ["failure", "skipped"], ["cancelled", "skipped"], ["success", "success"]]) {
        assert.throws(() => selectBuildDigest(primary, original, retry, retried), /signing and deployment refused/u);
    }
    for (const digest of ["", "latest", "sha256:abc", original + "\ninjected=true"]) {
        assert.throws(() => selectBuildDigest("success", digest, "skipped", ""), /valid immutable image digest/u);
        assert.throws(() => selectBuildDigest("failure", original, "success", digest), /valid immutable image digest/u);
    }
});

test("digest CLI publishes one successful output and none for a failed retry", async () => {
    const directory = mkdtempSync(join(tmpdir(), "fooddiary-retry-test-"));
    const script = fileURLToPath(new URL("./deploy-build-retry.mjs", import.meta.url));
    const output = join(directory, "output");
    const original = "sha256:" + "1".repeat(64);
    const retried = "sha256:" + "2".repeat(64);
    try {
        for (const [primary, retry, expected] of [["success", "skipped", original], ["failure", "success", retried], ["failure", "failure", null]]) {
            writeFileSync(output, "");
            const result = spawnSync(process.execPath, [script, "select"], {
                encoding: "utf8", env: { ...process.env, GITHUB_OUTPUT: output,
                    PRIMARY_OUTCOME: primary, PRIMARY_DIGEST: original, RETRY_OUTCOME: retry, RETRY_DIGEST: retried },
            });
            assert.equal(result.status, expected ? 0 : 1, result.stderr);
            assert.equal(readFileSync(output, "utf8"), expected ? `digest=${expected}\n` : "");
        }
    } finally {
        assert.equal(dirname(directory), tmpdir());
        rmSync(directory, { recursive: true });
    }
});

test("parallel scheduling includes every production image and its existing Dockerfile", async () => {
    const expected = ["api", "telegram-bot", "initializer", "job-manager", "mail-relay", "mailrelay-initializer",
        "mail-inbox", "mailinbox-postgres-tls-init", "mailinbox-initializer", "bugtriage", "client", "tech-radar"];
    assert.deepEqual(images.map(image => image.name).sort(), expected.sort());
    assert.equal(new Set(images.map(image => image.name)).size, 12);
    assert.equal(createMatrix().include.length, 12);
    for (const { file, context } of images) {
        assert.ok(await readFile(new URL(`../../${file}`, import.meta.url), "utf8"));
        assert.ok([".", "FoodDiary.Web.Client", "FoodDiary.TechRadar"].includes(context));
    }
});

test("all workers wait for authorization and server waits for the whole successful matrix", () => {
    const workers = job("build-images");
    const server = job("deploy");
    assert.match(workers, /needs: authorize/u);
    assert.match(workers, /max-parallel: 6/u);
    assert.match(workers, /fail-fast: false/u);
    assert.match(workers, /fromJSON\(needs\.authorize\.outputs\.matrix\)/u);
    assert.match(server, /needs: \[authorize, build-images\]/u);
    assert.doesNotMatch(server.split("    steps:")[0], /always\(\)|continue-on-error/u);
    assert.doesNotMatch(workers, /SSH_KEY|Setup SSH|Deploy to server/u);
    assert.match(job("authorize"), /contents: read/u);
    assert.doesNotMatch(job("authorize"), /packages: write|id-token: write/u);
});

test("each worker signs its image index before uploading a same-run receipt", () => {
    const workers = job("build-images");
    assert.match(workers, /provenance: mode=max/u);
    assert.match(workers, /sbom: true/u);
    assert.match(workers, /cache-from: type=gha,scope=production-\$\{\{ matrix\.image \}\}/u);
    assert.match(workers, /cache-to: type=gha,scope=production-\$\{\{ matrix\.image \}\},mode=max/u);
    assert.ok(workers.indexOf("sign-and-verify-images.sh") < workers.indexOf("deploy-images.mjs record"));
    assert.ok(workers.indexOf("deploy-images.mjs record") < workers.indexOf("actions/upload-artifact@"));
    assert.match(workers, /name: production-image-\$\{\{ github\.run_id \}\}-\$\{\{ matrix\.image \}\}/u);
    assert.match(workers, /overwrite: true/u);
    assert.match(workers, /if-no-files-found: error/u);
});

test("server rechecks source and verifies the complete manifest before any SSH", () => {
    const server = job("deploy");
    const source = server.indexOf("- name: Authorize deployment source");
    const checkout = server.indexOf("- name: Checkout code");
    const collect = server.indexOf("deploy-images.mjs collect");
    const verify = server.indexOf("cosign verify");
    const outputs = server.indexOf('cat "${RUNNER_TEMP}/image-digests.env" >> "$GITHUB_OUTPUT"');
    const ssh = server.indexOf("- name: Setup SSH");
    assert.ok(source >= 0 && source < checkout && checkout < collect && collect < verify && verify < outputs && outputs < ssh);
    assert.match(server, /pattern: production-image-\$\{\{ github\.run_id \}\}-\*/u);
    assert.match(server, /--certificate-identity "https:\/\/github\.com\/\$\{GITHUB_REPOSITORY\}\/\.github\/workflows\/deploy\.yml@refs\/heads\/master"/u);
    for (const { name } of images) assert.ok(server.includes(`steps.images.outputs.${name.replaceAll("-", "_")}`), name);
});

test("build failures and server failures retain their own notification context", () => {
    assert.match(job("deploy"), /- name: Notify Telegram \(failure\)/u);
    const notification = job("notify-build-failure");
    assert.match(notification, /needs: \[authorize, build-images\]/u);
    assert.match(notification, /always\(\)/u);
    assert.doesNotMatch(notification, /steps\.commit_meta|SSH_KEY/u);
});

test("complete shuffled receipts resolve to the exact original image digests", () => {
    const expected = receipts();
    assert.deepEqual(collectReceipts([...expected].reverse(), identity), expected.map(({ name, digest }) => ({ name, digest })));
});

for (const [name, change, error] of [
    ["missing image", rows => rows.slice(1), /Incomplete/u],
    ["duplicate image", rows => [...rows.slice(0, -1), rows[0]], /Duplicate/u],
    ["unknown image", rows => [{ ...rows[0], name: "injected" }, ...rows.slice(1)], /Unknown/u],
    ["foreign revision", rows => [{ ...rows[0], sourceSha: "b".repeat(40) }, ...rows.slice(1)], /sourceSha/u],
    ["foreign run", rows => [{ ...rows[0], runId: "9999" }, ...rows.slice(1)], /runId/u],
    ["future attempt", rows => [{ ...rows[0], runAttempt: "3" }, ...rows.slice(1)], /Future/u],
    ["invalid attempt", rows => [{ ...rows[0], runAttempt: "0" }, ...rows.slice(1)], /attempt/u],
    ["unknown schema", rows => [{ ...rows[0], schemaVersion: 2 }, ...rows.slice(1)], /schema/u],
    ["malformed digest", rows => [{ ...rows[0], digest: "sha256:abc\nINJECT=1" }, ...rows.slice(1)], /digest/u],
    ["mutable tag", rows => [{ ...rows[0], digest: "latest" }, ...rows.slice(1)], /digest/u],
]) {
    test(`manifest rejects ${name}`, () => assert.throws(() => collectReceipts(change(receipts()), identity), error));
}

test("rerunning failed workers can reuse successful receipts from the same run and commit", () => {
    const previous = receipts().map(receipt => ({ ...receipt, runAttempt: "1" }));
    assert.equal(collectReceipts(previous, identity).length, 12);
});

test("CLI emits all twelve outputs atomically and emits none for an invalid receipt set", () => {
    const directory = mkdtempSync(join(tmpdir(), "fooddiary-deploy-test-"));
    try {
        const rows = receipts();
        for (const receipt of rows) writeFileSync(join(directory, `${receipt.name}.json`), JSON.stringify(receipt));
        const run = () => spawnSync(process.execPath, [helper, "collect", directory], {
            encoding: "utf8", env: { ...process.env, SOURCE_COMMIT_SHA: identity.sourceSha, GITHUB_RUN_ID: identity.runId, GITHUB_RUN_ATTEMPT: identity.runAttempt },
        });
        const valid = run();
        assert.equal(valid.status, 0, valid.stderr);
        assert.equal(valid.stdout.trim().split("\n").length, 12);
        assert.ok(valid.stdout.includes(`mail_relay=${rows.find(row => row.name === "mail-relay").digest}`));
        writeFileSync(join(directory, "api.json"), JSON.stringify({ ...rows[0], sourceSha: "b".repeat(40) }));
        const invalid = run();
        assert.notEqual(invalid.status, 0);
        assert.equal(invalid.stdout, "");
        assert.match(invalid.stderr, /Foreign image receipt sourceSha/u);
    } finally {
        assert.equal(dirname(directory), tmpdir());
        rmSync(directory, { recursive: true });
    }
});
