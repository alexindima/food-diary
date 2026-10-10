import test from "node:test";
import assert from "node:assert/strict";
import { randomUUID, createHash } from "node:crypto";
import {
    mkdirSync,
    writeFileSync,
    unlinkSync,
    readFileSync,
    existsSync,
} from "node:fs";
import { join } from "node:path";
import { redactDiagnostic } from "./diagnostic-redaction.mjs";
import { summarizeEvent, compareTrials } from "./agent-trials.mjs";
import {
    evidenceFile,
    nextTaskAction,
    startCheck,
    cancelCheck,
} from "./task-tools.mjs";
import { repositoryRoot } from "./generate-feature.mjs";
import { serverDiagnosticEvents } from "./runtime-diagnostics.mjs";
import { checkAgentRegressions, promoteRegression } from "./agent-feedback.mjs";

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
