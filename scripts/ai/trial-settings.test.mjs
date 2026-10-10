import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import {
    trialOptions,
    sessionModelReceipt,
    sessionExecutionRejection,
    confirmedModelSettings,
    executionStartFailed,
    trialArguments,
    trialEnvironment,
} from "./trial-settings.mjs";

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
        trialOptions(["--model", "model", "--repetitions", "5"]).repetitions,
        5,
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
    const args = trialArguments("owned-candidate", {
        model: "pinned",
        reasoningEffort: "medium",
    });
    assert.ok(args.includes('default_permissions=":workspace"'));
    assert.equal(args.includes("--sandbox"), false);
    assert.ok(args.includes("--ignore-rules"));
    assert.equal(args[args.indexOf("-C") + 1], "owned-candidate");
    assert.equal(
        args.some((value) => /bypass|danger-full/u.test(value)),
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
