import { createReadStream, existsSync, lstatSync, readdirSync } from "node:fs";
import { homedir } from "node:os";
import { join, resolve } from "node:path";
import { createInterface } from "node:readline";

function windowsSandboxArguments(implementation, platform) {
    if (
        implementation !== undefined &&
        !["mxc", "elevated", "unelevated"].includes(implementation)
    )
        throw new Error("Unsupported Windows sandbox implementation");
    if (platform === "win32" && !implementation)
        throw new Error(
            "Specify --windows-sandbox from the approved Windows configuration when user config is ignored",
        );
    return platform === "win32"
        ? ["-c", `windows.sandbox="${implementation}"`]
        : [];
}

export function trialOptions(args, { platform = process.platform } = {}) {
    const options = {
        repetitions: 1,
        timeoutMinutes: 20,
        reasoningEffort: "medium",
    };
    const keys = {
        "--model": "model",
        "--reasoning": "reasoningEffort",
        "--repetitions": "repetitions",
        "--timeout-minutes": "timeoutMinutes",
        "--case": "caseId",
        "--windows-sandbox": "windowsSandbox",
    };
    for (let index = 0; index < args.length; index += 2) {
        const key = keys[args[index]];
        const value = args[index + 1];
        if (!key || !value || value.startsWith("--"))
            throw new Error(
                "Expected --model, --reasoning, --repetitions, --timeout-minutes, --case or --windows-sandbox with a value",
            );
        options[key] = ["repetitions", "timeoutMinutes"].includes(key)
            ? Number(value)
            : value;
    }
    if (!/^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$/u.test(options.model ?? ""))
        throw new Error("Specify an explicit --model for reproducible trials");
    if (
        !["low", "medium", "high", "xhigh", "max", "ultra"].includes(
            options.reasoningEffort,
        )
    )
        throw new Error("Unsupported reasoning effort");
    if (
        !Number.isInteger(options.repetitions) ||
        options.repetitions < 1 ||
        options.repetitions > 20
    )
        throw new Error("Repetitions must be 1-20");
    if (
        !Number.isFinite(options.timeoutMinutes) ||
        options.timeoutMinutes < 1 ||
        options.timeoutMinutes > 30
    )
        throw new Error("Trial timeout must be 1-30 minutes");
    windowsSandboxArguments(options.windowsSandbox, platform);
    return options;
}

export function confirmedModelSettings(settings, receipt, events) {
    const result = { ...settings };
    const observed = receipt?.model ?? result.actualModel;
    delete result.actualModel;
    if (
        events.some((event) => event.event === "turn.completed") &&
        typeof observed === "string" &&
        observed.length > 0
    ) {
        result.actualModel = observed;
    }
    return result;
}

export function trialEnvironment(environment = process.env) {
    const result = { ...environment };
    // A standalone evaluation owns its sandbox and must not attach to its parent app chat.
    for (const key of [
        "CODEX_PERMISSION_PROFILE",
        "CODEX_CLI_PATH",
        "CODEX_VERSION",
        "CODEX_CI",
        "CODEX_MCP_NODE_PATH",
        "CODEX_SHELL",
        "CODEX_WINDOWS_SANDBOX_PACKAGE_FAMILY",
        "CODEX_SAGE_BACKFILL_TRACKER_TAB_REUSE",
        "CODEX_THREAD_ID",
        "CODEX_SESSION_ID",
        "CODEX_INTERNAL_ORIGINATOR_OVERRIDE",
        "CODEX_APP_TOOLS_PIPE_PATH",
        "CODEX_TASK_WORKSPACE_VERIFYING_IDENTITY",
    ]) {
        delete result[key];
    }
    return result;
}

export function trialArguments(
    candidate,
    { model, reasoningEffort, windowsSandbox },
    { platform = process.platform } = {},
) {
    return [
        "-a",
        "never",
        "exec",
        "-c",
        'default_permissions=":workspace"',
        "-c",
        'web_search="disabled"',
        ...windowsSandboxArguments(windowsSandbox, platform),
        "--ignore-user-config",
        "--ignore-rules",
        "--model",
        model,
        "-c",
        `model_reasoning_effort=${reasoningEffort}`,
        "--json",
        "--color",
        "never",
        "-C",
        candidate,
        "-",
    ];
}

export function executionStartFailed(item) {
    // -1 is the CLI's failure-to-start envelope. Some versions omit its reason in JSONL.
    return item?.type === "command_execution" && item.exit_code === -1;
}

// Read only this runner's session, never arbitrary histories or authentication files.
// Session metadata is a best-effort observation; an explicit request alone is not proof.
function sessionDates(startedAtUtc) {
    const dates = new Set();
    for (const timestamp of [startedAtUtc, new Date().toISOString()]) {
        if (typeof timestamp !== "string") continue;
        const instant = new Date(timestamp).getTime();
        if (!Number.isFinite(instant)) continue;
        // CLI session directories can use the host's calendar date across UTC midnight.
        for (const offset of [0, -1, 1])
            dates.add(
                new Date(instant + offset * 86_400_000)
                    .toISOString()
                    .slice(0, 10),
            );
    }
    return [...dates];
}

function isPolicyRejection(output) {
    const texts =
        typeof output === "string"
            ? [output]
            : Array.isArray(output)
              ? output
                    .filter(
                        (block) =>
                            ["input_text", "output_text", "text"].includes(
                                block?.type,
                            ) && typeof block.text === "string",
                    )
                    .map((block) => block.text)
              : [];
    return texts.some((text) =>
        /^\s*(?:Script error:\s*)?(exec_command failed: CreateProcess[\s\S]*blocked by policy|patch rejected:[\s\S]*rejected by user approval settings)/u.test(
            text,
        ),
    );
}

async function inspectOwnedSession(
    threadId,
    candidate,
    startedAtUtc,
    sessionRoot = join(
        process.env.CODEX_HOME ?? join(homedir(), ".codex"),
        "sessions",
    ),
    observation = "model",
) {
    if (!/^[a-f0-9-]{36}$/u.test(threadId ?? "")) return null;
    for (const date of sessionDates(startedAtUtc)) {
        const directory = join(sessionRoot, ...date.split("-"));
        if (!existsSync(directory) || lstatSync(directory).isSymbolicLink())
            continue;
        for (const filename of readdirSync(directory)) {
            if (!filename.endsWith(`-${threadId}.jsonl`)) continue;
            const path = join(directory, filename);
            if (lstatSync(path).isSymbolicLink()) continue;
            let owned = false;
            let bytes = 0;
            const stream = createReadStream(path, { encoding: "utf8" });
            const lines = createInterface({
                input: stream,
                crlfDelay: Infinity,
            });
            try {
                for await (const line of lines) {
                    bytes += Buffer.byteLength(line);
                    if (bytes > 2 * 1024 * 1024) break;
                    let event;
                    try {
                        event = JSON.parse(line);
                    } catch {
                        continue;
                    }
                    if (event.type === "session_meta") {
                        owned =
                            event.payload?.id === threadId &&
                            resolve(event.payload?.cwd ?? "") ===
                                resolve(candidate);
                        if (!owned) break;
                    }
                    if (
                        observation === "execution-failure" &&
                        owned &&
                        [
                            "function_call_output",
                            "custom_tool_call_output",
                        ].includes(event.payload?.type) &&
                        isPolicyRejection(event.payload.output)
                    ) {
                        return {
                            source: "owned-session-tool-rejection",
                            threadId,
                        };
                    }
                    if (
                        observation === "model" &&
                        owned &&
                        event.type === "turn_context" &&
                        /^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$/u.test(
                            event.payload?.model ?? "",
                        )
                    ) {
                        return {
                            model: event.payload.model,
                            reasoningEffort: event.payload.effort ?? null,
                            source: "owned-session-turn-context",
                            threadId,
                        };
                    }
                }
            } finally {
                lines.close();
                stream.destroy();
            }
        }
    }
    return null;
}

export function sessionModelReceipt(
    threadId,
    candidate,
    startedAtUtc,
    sessionRoot,
) {
    return inspectOwnedSession(threadId, candidate, startedAtUtc, sessionRoot);
}

export function sessionExecutionRejection(
    threadId,
    candidate,
    startedAtUtc,
    sessionRoot,
) {
    return inspectOwnedSession(
        threadId,
        candidate,
        startedAtUtc,
        sessionRoot,
        "execution-failure",
    );
}
