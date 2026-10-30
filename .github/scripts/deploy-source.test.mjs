import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { mkdtempSync, writeFileSync, rmSync } from "node:fs";
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

const identity = { sourceSha: "a".repeat(40), runId: "1234", runAttempt: "2" };
const receipts = () => images.map(({ name }, index) => createReceipt({
    name, digest: `sha256:${(index + 1).toString(16).padStart(64, "0")}`, ...identity,
}));
const helper = fileURLToPath(new URL("./deploy-images.mjs", import.meta.url));
const job = name => workflow.match(new RegExp(`^  ${name}:\\r?\\n([\\s\\S]*?)(?=^  [\\w-]+:\\r?$|(?![\\s\\S]))`, "mu"))?.[1];

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
