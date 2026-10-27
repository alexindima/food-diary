import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { trialSourceFingerprints } from "./trial-fingerprints.mjs";
import {
    trialOptions,
    sessionModelReceipt,
    sessionExecutionRejection,
    confirmedModelSettings,
    executionStartFailed,
    trialArguments,
    trialEnvironment,
} from "./trial-settings.mjs";

test("trial fingerprints change when execution or grading helpers change", () => {
    const root = mkdtempSync(join(tmpdir(), "fd-trial-sources-"));
    const sources = [
        "agent-trials.mjs",
        "trial-settings.mjs",
        "trial-fingerprints.mjs",
        "generate-feature.mjs",
        "diagnostic-redaction.mjs",
        "candidate-contract-check.cjs",
        "coding-evals.mjs",
        "coding-probe-isolation.mjs",
        "coding-probe-worker.cjs",
    ];
    mkdirSync(join(root, "scripts/ai"), { recursive: true });
    for (const source of sources)
        writeFileSync(join(root, "scripts/ai", source), source);
    try {
        const baseline = trialSourceFingerprints(root);
        writeFileSync(
            join(root, "scripts/ai/trial-settings.mjs"),
            "changed execution settings",
        );
        const changedHarness = trialSourceFingerprints(root);
        assert.notEqual(
            changedHarness.harnessHash,
            baseline.harnessHash,
            "Execution helper changes must invalidate the trial cohort",
        );
        assert.equal(
            changedHarness.graderEngineHash,
            baseline.graderEngineHash,
        );
        writeFileSync(
            join(root, "scripts/ai/coding-probe-worker.cjs"),
            "changed assertions",
        );
        assert.notEqual(
            trialSourceFingerprints(root).graderEngineHash,
            baseline.graderEngineHash,
            "Runtime assertion changes must invalidate the grader cohort",
        );
        writeFileSync(
            join(root, "scripts/ai/candidate-contract-check.cjs"),
            "changed public check",
        );
        assert.notEqual(
            trialSourceFingerprints(root).harnessHash,
            changedHarness.harnessHash,
        );
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("trial options require a pinned model and bounded explicit sample count", () => {
    assert.throws(() => trialOptions([]), /explicit --model/);
    assert.throws(
        () => trialOptions(["--model", "model", "--repetitions", "0"]),
        /Repetitions/,
    );
    assert.throws(
        () =>
            trialOptions(["--model", "model", "--timeout-minutes", "Infinity"]),
        /timeout/,
    );
    assert.throws(
        () => trialOptions(["--model", "model", "--reasoning", "arbitrary"]),
        /reasoning/,
    );
    assert.equal(
        trialOptions([
            "--model",
            "model",
            "--repetitions",
            "5",
            "--windows-sandbox",
            "unelevated",
        ]).repetitions,
        5,
    );
});

test("Windows trials require an explicit supported sandbox when user configuration is ignored", () => {
    assert.throws(
        () => trialOptions(["--model", "pinned"], { platform: "win32" }),
        /--windows-sandbox/,
        "Windows sandbox selection must not disappear with user config",
    );
    for (const implementation of ["mxc", "elevated", "unelevated"])
        assert.equal(
            trialOptions(
                ["--model", "pinned", "--windows-sandbox", implementation],
                { platform: "win32" },
            ).windowsSandbox,
            implementation,
        );
    for (const implementation of [
        "disabled",
        "danger-full-access",
        "arbitrary",
    ])
        assert.throws(
            () =>
                trialOptions(
                    ["--model", "pinned", "--windows-sandbox", implementation],
                    { platform: "win32" },
                ),
            /Windows sandbox/,
        );
    assert.equal(
        trialOptions(["--model", "pinned"], { platform: "linux" })
            .windowsSandbox,
        undefined,
    );
});

test("a policy-rejected command stops model retries, while ordinary test failures do not", () => {
    assert.equal(
        executionStartFailed({ type: "command_execution", exit_code: -1 }),
        true,
    );
    assert.equal(
        executionStartFailed({
            type: "command_execution",
            exit_code: -1,
            aggregated_output: "CreateProcess Rejected: blocked by policy",
        }),
        true,
    );
    assert.equal(
        executionStartFailed({
            type: "command_execution",
            exit_code: 1,
            aggregated_output: "Assertion failed: read-only sandbox scenario",
        }),
        false,
    );
    assert.equal(
        executionStartFailed({
            type: "agent_message",
            text: "blocked by policy",
        }),
        false,
    );
});

test("standalone trials exclude parent chat permissions and tools while preserving authentication location", () => {
    const parent = {
        CODEX_PERMISSION_PROFILE: "parent-read-only",
        CODEX_CLI_PATH: "parent-app-executable",
        CODEX_THREAD_ID: "parent",
        CODEX_APP_TOOLS_PIPE_PATH: "parent-apps",
        CODEX_HOME: "existing-store",
        PATH: "tools",
    };
    const child = trialEnvironment(parent);
    assert.equal(child.CODEX_PERMISSION_PROFILE, undefined);
    assert.equal(child.CODEX_CLI_PATH, undefined);
    assert.equal(child.CODEX_THREAD_ID, undefined);
    assert.equal(child.CODEX_APP_TOOLS_PIPE_PATH, undefined);
    assert.equal(child.CODEX_HOME, "existing-store");
    assert.equal(parent.CODEX_PERMISSION_PROFILE, "parent-read-only");
    const args = trialArguments(
        "owned-candidate",
        {
            model: "pinned",
            reasoningEffort: "medium",
            windowsSandbox: "unelevated",
        },
        { platform: "win32" },
    );
    assert.ok(args.includes('default_permissions=":workspace"'));
    assert.ok(
        args.includes('windows.sandbox="unelevated"'),
        "The selected Windows sandbox must reach the isolated CLI",
    );
    assert.equal(args.includes("--sandbox"), false);
    assert.ok(args.includes("--ignore-rules"));
    assert.equal(args[args.indexOf("-C") + 1], "owned-candidate");
    assert.equal(
        args.some((value) => /bypass|danger-full/u.test(value)),
        false,
    );
    assert.equal(
        trialArguments(
            "owned-candidate",
            { model: "pinned", reasoningEffort: "medium" },
            { platform: "linux" },
        ).some((value) => value.startsWith("windows.sandbox=")),
        false,
    );
});

test("a rejected model request never becomes a confirmed actual model", () => {
    const settings = {
        requestedModel: "requested",
        actualModel: "startup-banner",
    };
    const receipt = { model: "configured" };
    assert.deepEqual(
        confirmedModelSettings(settings, receipt, [{ event: "turn.failed" }]),
        { requestedModel: "requested" },
    );
    assert.equal(
        confirmedModelSettings(settings, receipt, [{ event: "turn.completed" }])
            .actualModel,
        "configured",
    );
});

test("model receipt requires a matching session and candidate, with no transcript returned", async () => {
    const root = mkdtempSync(join(tmpdir(), "fd-trial-receipt-"));
    const id = "12345678-1234-1234-1234-123456789abc";
    const candidate = join(root, "candidate");
    const date = "2026-10-10";
    const directory = join(root, "2026", "10", "10");
    mkdirSync(directory, { recursive: true });
    const path = join(directory, `rollout-${id}.jsonl`);
    const write = (cwd, model) =>
        writeFileSync(
            path,
            [
                { type: "session_meta", payload: { id, cwd } },
                {
                    type: "response_item",
                    payload: {
                        content: "Private reasoning must never be returned",
                    },
                },
                { type: "turn_context", payload: { model, effort: "medium" } },
            ]
                .map((value) => JSON.stringify(value))
                .join("\n"),
        );
    try {
        write(join(root, "foreign"), "observed-model");
        assert.equal(
            await sessionModelReceipt(id, candidate, date, root),
            null,
        );
        write(candidate, "observed-model");
        assert.deepEqual(await sessionModelReceipt(id, candidate, date, root), {
            model: "observed-model",
            reasoningEffort: "medium",
            source: "owned-session-turn-context",
            threadId: id,
        });
        assert.equal(
            await sessionModelReceipt("../unrelated", candidate, date, root),
            null,
        );
        write(candidate, undefined);
        assert.equal(
            await sessionModelReceipt(id, candidate, date, root),
            null,
        );
        write(candidate, "observed-model");
        writeFileSync(
            path,
            "\n" +
                JSON.stringify({
                    type: "response_item",
                    payload: {
                        type: "function_call_output",
                        output: "exec_command failed: CreateProcess { Rejected: blocked by policy }",
                    },
                }),
            { flag: "a" },
        );
        assert.deepEqual(
            await sessionExecutionRejection(id, candidate, date, root),
            { source: "owned-session-tool-rejection", threadId: id },
        );
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("owned session receipts survive local dates on either side of UTC midnight", async () => {
    const root = mkdtempSync(join(tmpdir(), "fd-trial-midnight-"));
    const id = "12345678-1234-1234-1234-123456789abc";
    const candidate = join(root, "candidate");
    try {
        for (const [timestamp, localDate] of [
            ["2026-10-10T22:30:00Z", "2026/10/11"],
            ["2026-10-10T01:30:00Z", "2026/10/09"],
            ["2026-12-31T23:30:00Z", "2027/01/01"],
        ]) {
            const directory = join(root, localDate);
            mkdirSync(directory, { recursive: true });
            writeFileSync(
                join(directory, `rollout-${id}.jsonl`),
                [
                    { type: "session_meta", payload: { id, cwd: candidate } },
                    {
                        type: "turn_context",
                        payload: { model: "observed-model", effort: "xhigh" },
                    },
                ]
                    .map((row) => JSON.stringify(row))
                    .join("\n"),
            );
            assert.equal(
                (await sessionModelReceipt(id, candidate, timestamp, root))
                    ?.model,
                "observed-model",
                "A local-date session must retain its model receipt",
            );
            rmSync(join(directory, `rollout-${id}.jsonl`));
        }
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("structured CLI tool outputs retain policy rejections without interpreting ordinary test failures", async () => {
    const root = mkdtempSync(join(tmpdir(), "fd-trial-structured-output-"));
    const id = "12345678-1234-1234-1234-123456789abc";
    const candidate = join(root, "candidate");
    const directory = join(root, "2026/10/10");
    mkdirSync(directory, { recursive: true });
    const command =
        "Script error:\nexec_command failed: CreateProcess { Rejected: blocked by policy }";
    const patch =
        "Script error:\npatch rejected: writing outside of the project; rejected by user approval settings";
    try {
        for (const [type, output, rejected] of [
            ["custom_tool_call_output", command, true],
            [
                "custom_tool_call_output",
                [
                    { type: "input_text", text: "Tool result" },
                    { type: "input_text", text: command },
                ],
                true,
            ],
            [
                "custom_tool_call_output",
                [{ type: "input_text", text: patch }],
                true,
            ],
            ["function_call_output", [{ type: "text", text: command }], true],
            [
                "function_call_output",
                [{ type: "output_text", text: patch }],
                true,
            ],
            [
                "custom_tool_call_output",
                [
                    {
                        type: "input_text",
                        text: "Assertion failed: expected blocked by policy",
                    },
                ],
                false,
            ],
            ["agent_message", [{ type: "input_text", text: command }], false],
        ]) {
            writeFileSync(
                join(directory, `rollout-${id}.jsonl`),
                [
                    { type: "session_meta", payload: { id, cwd: candidate } },
                    { type: "response_item", payload: { type, output } },
                ]
                    .map((row) => JSON.stringify(row))
                    .join("\n"),
            );
            assert.equal(
                Boolean(
                    await sessionExecutionRejection(
                        id,
                        candidate,
                        "2026-10-10",
                        root,
                    ),
                ),
                rejected,
                "Structured tool policy rejection must be detected",
            );
        }
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});
