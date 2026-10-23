import test from "node:test";
import assert from "node:assert/strict";
import { randomUUID, createHash } from "node:crypto";
import {
    mkdirSync,
    writeFileSync,
    unlinkSync,
    readFileSync,
    existsSync,
    mkdtempSync,
    rmSync,
    utimesSync,
} from "node:fs";
import { join, resolve, sep } from "node:path";
import { tmpdir } from "node:os";
import { createServer } from "node:http";
import { redactDiagnostic } from "./diagnostic-redaction.mjs";
import { summarizeEvent, compareTrials } from "./agent-trials.mjs";
import { isAllowedCandidatePath } from "./coding-evals.mjs";
import { sanitizeJourneyReport } from "./journey-evidence.mjs";
import {
    evidenceFile,
    nextTaskAction,
    startCheck,
    cancelCheck,
    taskDiagnostics,
} from "./task-tools.mjs";
import { repositoryRoot } from "./generate-feature.mjs";
import {
    collectMailDiagnostics,
    serverDiagnosticEvents,
} from "./runtime-diagnostics.mjs";
import { processIdentity } from "./task-runtime.mjs";
import { checkAgentRegressions, promoteRegression } from "./agent-feedback.mjs";

test("quiet live checks remain running and expose actual command and wrapper exits separately", () => {
    const id = randomUUID();
    const directory = join(repositoryRoot, ".artifacts/agent-checks", id);
    const logRelative = `.artifacts/llm-wiki/tasks/diagnostics-${id}/logs/check.log`;
    const canonicalLog = join(repositoryRoot, logRelative);
    mkdirSync(directory, { recursive: true });
    mkdirSync(join(repositoryRoot, `.artifacts/llm-wiki/tasks/diagnostics-${id}/logs`), { recursive: true });
    writeFileSync(canonicalLog, "Still executing a canonical test\n");
    const quietTime = new Date(Date.now() - 90000);
    utimesSync(canonicalLog, quietTime, quietTime);
    const record = { id, checkout: repositoryRoot, status: "running", startedAtUtc: new Date(Date.now() - 120000).toISOString(), worker: { pid: process.pid, identity: processIdentity(process.pid) }, executionLogPath: logRelative };
    writeFileSync(join(directory, "job.json"), JSON.stringify(record));
    const live = taskDiagnostics(undefined, id).job;
    assert.equal(live.status, "running");
    assert.equal(live.workerAlive, true);
    assert.equal(live.progress.stage, "executing-check");
    assert.ok(live.progress.secondsSinceLastOutput >= 90);
    assert.equal(live.progress.silenceMeansHung, false);
    writeFileSync(`${canonicalLog}.execution.json`, JSON.stringify({ exitCode: 1 }));
    writeFileSync(join(directory, "job.json"), JSON.stringify({ ...record, status: "failed", exitCode: 0, finishedAtUtc: new Date().toISOString() }));
    const failed = taskDiagnostics(undefined, id).job;
    assert.equal(failed.status, "failed");
    assert.equal(failed.progress.stage, "finished");
    assert.equal(failed.progress.wrapperExitCode, 0);
    assert.equal(failed.progress.commandExitCode, 1);
});

test("dead worker recovery releases only its own lock and retains unverified status", () => {
    const id = randomUUID();
    const evidencePath = `.artifacts/llm-wiki/dead-worker-${id}.json`;
    const directory = join(repositoryRoot, ".artifacts/agent-checks", id);
    mkdirSync(directory, { recursive: true });
    writeFileSync(
        join(directory, "job.json"),
        JSON.stringify({
            id,
            checkout: repositoryRoot,
            evidencePath,
            status: "running",
            worker: { pid: 99999999, identity: "old" },
        }),
    );
    const lock = join(
        repositoryRoot,
        ".artifacts/agent-checks",
        `${createHash("sha256").update(evidencePath).digest("hex")}.lock`,
    );
    writeFileSync(lock, id);
    assert.equal(cancelCheck(id).status, "interrupted");
    assert.equal(existsSync(lock), false);
});

test("a file-scoped coding task rejects similarly named siblings and preserves directory scopes", () => {
    assert.equal(
        isAllowedCandidatePath("src/owned.ts", ["src/owned.ts"]),
        true,
    );
    assert.equal(
        isAllowedCandidatePath("src/owned.ts.backup", ["src/owned.ts"]),
        false,
    );
    assert.equal(
        isAllowedCandidatePath("src/feature/file.ts", ["src/feature/"]),
        true,
    );
    assert.equal(
        isAllowedCandidatePath("src/feature-neighbor/file.ts", [
            "src/feature/",
        ]),
        false,
    );
});

test("live journey reports retain test counts and remove credential values from nested errors", () => {
    const report = sanitizeJourneyReport({
        stats: { expected: 3 },
        errors: [
            {
                message:
                    "Authorization: Bearer abc.def.ghi password=secret123 user@example.org",
            },
        ],
    });
    assert.equal(report.stats.expected, 3);
    for (const value of ["abc.def.ghi", "secret123", "user@example.org"])
        assert.equal(JSON.stringify(report).includes(value), false);
});

test("review promotion refuses an empty evidence proposal", () => {
    const path = join(
        repositoryRoot,
        `.artifacts/agent-feedback/empty-${randomUUID()}.proposal.json`,
    );
    mkdirSync(join(repositoryRoot, ".artifacts/agent-feedback"), {
        recursive: true,
    });
    writeFileSync(
        path,
        JSON.stringify({
            status: "awaiting-review",
            rule: "production-change-required",
            evidence: [],
        }),
    );
    try {
        assert.throws(
            () => promoteRegression(path, "reviewer", "reason"),
            /source trial evidence/u,
        );
    } finally {
        unlinkSync(path);
    }
});

test("runtime logs distinguish failed HTTP status from bundle-size output", () => {
    const events = serverDiagnosticEvents(
        "chunk-example | 539 bytes\nHTTP/1.1 GET /api/missing 404\nHTTP GET unmatched responded 404 in 0.45 ms\nError: read ECONNRESET",
    );
    assert.equal(events.length, 3);
    assert.ok(events[0].includes("404"));
});

test("mail diagnostics require matching task identity and do not infer delivery from provider readiness", async () => {
    const root = mkdtempSync(join(tmpdir(), "fd-mail-diagnostics-"));
    const id = randomUUID();
    let health = {
        task: id,
        status: "ready",
        providers: "disabled",
        private: "secret-sentinel",
    };
    let requests = 0;
    const server = createServer((request, response) => {
        requests++;
        assert.equal(request.method, "GET");
        assert.equal(request.url, "/health");
        response.setHeader("content-type", "application/json");
        response.end(JSON.stringify(health));
    });
    await new Promise((ready) => server.listen(0, "127.0.0.1", ready));
    try {
        const directory = join(root, ".artifacts/task-runtime/mail");
        mkdirSync(directory, { recursive: true });
        const state = {
            id,
            checkout: root,
            ports: { providers: server.address().port },
            processes: [
                {
                    role: "providers",
                    pid: process.pid,
                    identity: processIdentity(process.pid),
                },
            ],
            integrations: {
                backgroundJobs: "not started",
                email: "secret-sentinel",
            },
        };
        const save = () =>
            writeFileSync(
                join(directory, "runtime.json"),
                JSON.stringify(state),
            );
        save();
        const result = await collectMailDiagnostics("mail", root);
        assert.deepEqual(result.provider, {
            processAlive: true,
            health: "ready",
            mode: "disabled-stub",
        });
        assert.equal(result.backgroundJobs, "not-started-at-launch");
        assert.equal(result.applicationOutbox, "not-observed");
        assert.equal(result.smtpDelivery, "unavailable-disabled-provider");
        assert.equal(result.endToEndDelivery, "not-verified");
        assert.equal(JSON.stringify(result).includes("secret-sentinel"), false);
        health = { ...health, task: randomUUID() };
        assert.equal(
            (await collectMailDiagnostics("mail", root)).provider.health,
            "identity-mismatch",
        );
        health = { ...health, task: id, providers: "enabled" };
        assert.equal(
            (await collectMailDiagnostics("mail", root)).smtpDelivery,
            "not-observed",
        );
        state.processes[0].identity = "reused-pid";
        save();
        assert.equal(
            (await collectMailDiagnostics("mail", root)).provider.processAlive,
            false,
        );
        assert.equal(requests, 3);
        state.processes[0].identity = null;
        save();
        assert.equal(
            (await collectMailDiagnostics("mail", root)).provider.processAlive,
            false,
        );
        state.id = undefined;
        save();
        await assert.rejects(
            collectMailDiagnostics("mail", root),
            /runtime identity/u,
        );
        assert.equal(requests, 3);
        state.checkout = root + "-foreign";
        save();
        await assert.rejects(
            collectMailDiagnostics("mail", root),
            /another checkout/u,
        );
    } finally {
        await new Promise((closed) => server.close(closed));
        assert.ok(resolve(root).startsWith(resolve(tmpdir()) + sep));
        assert.ok(root.includes("fd-mail-diagnostics-"));
        rmSync(root, { recursive: true, force: true });
    }
});

test("reviewed outcome regressions reject unchanged coding candidates", () => {
    const registry = { rules: [{ id: "production-change-required" }] };
    assert.equal(
        checkAgentRegressions({ grade: { changedPaths: [] } }, registry)[0]
            .passed,
        false,
    );
    assert.equal(
        checkAgentRegressions(
            { grade: { changedPaths: ["owning-capability.ts"] } },
            registry,
        )[0].passed,
        true,
    );
});

test("diagnostics retain the error and location while removing credential and query values", () => {
    const value = redactDiagnostic(
        "error src/a.ts:12 password=secret123 Authorization: Bearer abc.def.ghi user@example.org http://u:p@127.0.0.1/api?code=private&date=2026-01-01",
    );
    assert.match(value, /src\/a.ts:12/u);
    for (const secret of [
        "secret123",
        "abc.def.ghi",
        "user@example.org",
        "u:p@",
        "code=private",
        "date=2026",
    ])
        assert.equal(value.includes(secret), false);
});

test("trace events include tool failures without full output or reasoning", () => {
    const value = summarizeEvent(
        {
            type: "item.completed",
            item: {
                type: "command_execution",
                command: "node check",
                exit_code: 1,
                aggregated_output:
                    "large irrelevant output\nerror path.ts:3 password=private",
            },
        },
        4,
    );
    assert.equal(value.exitCode, 1);
    assert.equal(value.sequence, 4);
    assert.equal(JSON.stringify(value).includes("irrelevant"), false);
    assert.equal(JSON.stringify(value).includes("private"), false);
    assert.equal(
        summarizeEvent(
            {
                type: "item.completed",
                item: { type: "reasoning", text: "internal" },
            },
            5,
        ).text,
        undefined,
    );
});

test("instruction comparison refuses unmatched dependencies and small-sample gain claims", () => {
    const row = {
        id: "usda",
        baseline: "abc",
        cliVersion: "same",
        instructionsHash: "same",
        harnessHash: "same",
        graderEngineHash: "same",
        graderHash: "same",
        corpusHash: "same",
        dependencyLockHash: "one",
        timeoutMinutes: 20,
        status: "completed",
        passed: true,
        durationMs: 12,
    };
    const result = compareTrials([
        { ...row, variant: "control" },
        { ...row, variant: "api-skill" },
        { ...row, dependencyLockHash: "two", variant: "api-skill" },
    ]);
    assert.equal(result.comparisons.length, 2);
    assert.equal(result.comparisons[0].control.samples, 1);
    assert.equal(result.comparisons[0].skill.samples, 1);
    assert.equal(result.comparisons[0].verdict, "insufficient-data");
});

test("task tools reject external evidence and choose a failed check before claiming completion", () => {
    const name = `agent-tool-fixture-${randomUUID()}`;
    const relativePath = `.artifacts/llm-wiki/${name}.json`;
    const path = join(repositoryRoot, relativePath);
    mkdirSync(join(repositoryRoot, ".artifacts/llm-wiki"), { recursive: true });
    writeFileSync(
        path,
        JSON.stringify({
            git: { base: "a".repeat(40) },
            change: { changedPaths: [] },
            checks: [
                {
                    id: "architecture-tests",
                    status: "failed",
                    reason: "Boundary failed",
                },
            ],
        }),
    );
    try {
        assert.throws(() => evidenceFile("../../secrets.json"));
        assert.throws(() =>
            evidenceFile(".artifacts/llm-wiki/../secrets.json"),
        );
        assert.equal(nextTaskAction(relativePath).state, "repair-required");
        assert.throws(() =>
            startCheck(relativePath, "arbitrary-shell-command"),
        );
        assert.throws(() =>
            startCheck(relativePath, "architecture-tests", 999),
        );
    } finally {
        unlinkSync(path);
    }
});

test("skill examples are grounded in current decoder and actual authorization tests", () => {
    const text = readFileSync(
        join(
            repositoryRoot,
            ".agents/skills/fooddiary-api-change/references/examples.md",
        ),
        "utf8",
    );
    const paths = [...text.matchAll(/(?:Source|Check): `([^`]+)`/gu)].map(
        (match) => match[1],
    );
    assert.equal(paths.length, 4);
    for (const path of paths)
        assert.ok(readFileSync(join(repositoryRoot, path), "utf8").length > 0);
    const example = /```typescript\n([\s\S]*?)\n```/u.exec(text)[1];
    assert.ok(
        readFileSync(join(repositoryRoot, paths[0]), "utf8")
            .replaceAll("\r\n", "\n")
            .includes(example.trim()),
    );
});
