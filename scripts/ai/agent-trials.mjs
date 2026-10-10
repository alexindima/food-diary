import { createHash } from "node:crypto";
import { spawn, spawnSync } from "node:child_process";
import {
    existsSync,
    readFileSync,
    writeFileSync,
    mkdirSync,
    readdirSync,
    copyFileSync,
    realpathSync,
} from "node:fs";
import { dirname, join, resolve } from "node:path";
import { pathToFileURL, fileURLToPath } from "node:url";
import { codingCase, prepareCase, gradeCase } from "./coding-evals.mjs";
import { repositoryRoot, containedPath } from "./generate-feature.mjs";
import { redactDiagnostic, errorLines } from "./diagnostic-redaction.mjs";
import { compareTrialRecords } from "./trial-comparison.mjs";

const sha = (value) => createHash("sha256").update(value).digest("hex");
const save = (path, value) =>
    writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`);

export function codexCommand() {
    if (process.platform !== "win32") return { file: "codex", prefix: [] };
    // Execute the npm JavaScript entry point directly; never interpolate a shell command.
    for (const directory of (process.env.PATH ?? "").split(";")) {
        const entry = join(
            directory,
            "node_modules/@openai/codex/bin/codex.js",
        );
        if (existsSync(entry))
            return { file: process.execPath, prefix: [entry] };
    }
    throw new Error("Codex CLI is unavailable on PATH");
}

export function summarizeEvent(event, sequence) {
    const item = event.item ?? {};
    const row = { sequence, event: event.type, itemType: item.type };
    if (item.type === "command_execution") {
        row.command = redactDiagnostic(item.command, 800);
        row.exitCode = item.exit_code;
        row.outputCharacters = String(item.aggregated_output ?? "").length;
        if (item.exit_code !== undefined && item.exit_code !== 0)
            row.errors = errorLines(item.aggregated_output ?? "");
    }
    if (item.type === "file_change")
        row.paths = (item.changes ?? []).map((change) =>
            redactDiagnostic(change.path),
        );
    if (item.type === "agent_message")
        row.message = redactDiagnostic(item.text ?? "", 2000);
    if (event.usage) row.usage = event.usage;
    if (event.type === "error" || event.type === "turn.failed")
        row.error = redactDiagnostic(
            event.message ?? event.error?.message ?? "Agent turn failed",
        );
    if (event.type === "thread.started") row.threadId = event.thread_id;
    return row;
}

export async function runTrial(
    id,
    { variant = "control", timeoutMinutes = 20 } = {},
) {
    if (!["control", "api-skill"].includes(variant))
        throw new Error("Unknown instruction variant");
    if (
        !Number.isFinite(timeoutMinutes) ||
        timeoutMinutes < 1 ||
        timeoutMinutes > 30
    )
        throw new Error("Trial timeout must be 1-30 minutes");
    const entry = codingCase(id);
    const prepared = prepareCase(id);
    const directory = dirname(prepared.candidate);
    const trialPath = join(directory, "trial.json");
    const eventsPath = join(directory, "events.jsonl");
    const instructions = `${readFileSync(join(repositoryRoot, "AGENTS.md"), "utf8")}\n\n${readFileSync(join(repositoryRoot, "FoodDiary.Web.Client/AGENTS.md"), "utf8")}`;
    writeFileSync(join(prepared.candidate, "AGENTS.md"), instructions);
    const contract = readFileSync(
        join(
            repositoryRoot,
            "Tooling/ai-development/coding-evals/graders",
            entry.grader,
        ),
        "utf8",
    );
    copyFileSync(
        join(repositoryRoot, "scripts/ai/candidate-contract-check.cjs"),
        join(prepared.candidate, "verify-contract.cjs"),
    );
    writeFileSync(join(prepared.candidate, "acceptance.ts"), contract);
    save(join(prepared.candidate, "contract-check.json"), {
        dependenciesPath: realpathSync(
            join(repositoryRoot, "FoodDiary.Web.Client/node_modules"),
        ),
    });
    let skill = "";
    if (variant === "api-skill") {
        skill = readFileSync(
            join(
                repositoryRoot,
                ".agents/skills/fooddiary-api-change/SKILL.md",
            ),
            "utf8",
        );
        const folder = join(
            prepared.candidate,
            ".agents/skills/fooddiary-api-change",
        );
        mkdirSync(folder, { recursive: true });
        writeFileSync(join(folder, "SKILL.md"), skill);
        const reference = readFileSync(
            join(
                repositoryRoot,
                ".agents/skills/fooddiary-api-change/references/examples.md",
            ),
            "utf8",
        );
        mkdirSync(join(folder, "references"), { recursive: true });
        writeFileSync(join(folder, "references/examples.md"), reference);
        skill += reference;
    }
    const probeValue = `${id}:${prepared.graderHash}`;
    const prompt = `${entry.task}\n\nThis is an isolated coding evaluation. Before exploring sources, create agent-write-probe.txt at the snapshot root containing exactly ${probeValue} to prove the actual session can write. This probe is explicitly allowed. Edit production files only in: ${entry.allowedPrefixes.join(", ")}. Preserve native values. No production services, network providers, commits, or external messages are needed. Do not inspect the parent repository or historical answers. The snapshot intentionally omits unrelated build projects, Wiki and dependency installation. Its AGENTS.md includes applicable repository and frontend rules. Work directly against the included production sources; full application builds are not available in this extracted snapshot. Run node verify-contract.cjs to check the public acceptance contract with actual dependency types before concluding. Do not replace Angular/RxJS/SDK types with simplified stubs.\n\nPublic acceptance contract (implement these exported capabilities; it contains no reference implementation):\n${contract}\n\nNative-value contract: ${entry.runtimeContract ?? "Keep the existing wire values and behavior unchanged."}\n\n${skill ? "Use $fooddiary-api-change from .agents/skills/fooddiary-api-change/SKILL.md. Read its examples only if useful." : ""}`;
    const command = codexCommand();
    for (const args of [
        ["init", "--quiet"],
        ["add", "."],
        [
            "-c",
            "user.name=FoodDiary Eval",
            "-c",
            "user.email=eval@local.invalid",
            "commit",
            "--quiet",
            "-m",
            "Isolated evaluation baseline",
        ],
    ]) {
        const initialized = spawnSync("git", args, {
            cwd: prepared.candidate,
            windowsHide: true,
            encoding: "utf8",
        });
        if (initialized.status !== 0)
            throw new Error(
                "Cannot initialize the isolated evaluation repository",
            );
    }
    const version = spawnSync(command.file, [...command.prefix, "--version"], {
        encoding: "utf8",
        windowsHide: true,
    });
    if (version.status !== 0) throw new Error("Cannot identify Codex CLI");
    const record = {
        schemaVersion: 1,
        kind: "real-coding-trial",
        id,
        variant,
        baseline: entry.baseline,
        candidate: prepared.candidate,
        cliVersion: version.stdout.trim(),
        instructionsHash: sha(instructions),
        skillHash: skill ? sha(skill) : null,
        promptHash: sha(prompt),
        graderHash: prepared.graderHash,
        corpusHash: prepared.corpusHash,
        dependencyLockHash: prepared.dependencyLockHash,
        timeoutMinutes,
        harnessHash: sha(readFileSync(fileURLToPath(import.meta.url))),
        graderEngineHash: sha(
            readFileSync(join(repositoryRoot, "scripts/ai/coding-evals.mjs")),
        ),
        candidateGitRoot: true,
        startedAtUtc: new Date().toISOString(),
        status: "running",
        modelTrial: true,
        interventions: 0,
        eventsPath,
        usage: null,
        settings: {
            permissionProfile: ":workspace",
            approvalPolicy: "never",
            ephemeral: true,
            ignoreUserConfig: true,
            model: "CLI default; actual identity recorded when reported",
        },
    };
    save(trialPath, record);
    writeFileSync(eventsPath, "");
    const started = performance.now();
    const rows = [];
    let pending = "";
    let timedOut = false;
    let outputBytes = 0;
    let failure = null;
    const child = spawn(
        command.file,
        [
            ...command.prefix,
            "-a",
            "never",
            "-c",
            'default_permissions=":workspace"',
            "exec",
            "--ignore-user-config",
            "--ephemeral",
            "--json",
            "--color",
            "never",
            "-C",
            prepared.candidate,
            "-",
        ],
        {
            cwd: prepared.candidate,
            windowsHide: true,
            stdio: ["pipe", "pipe", "pipe"],
        },
    );
    const terminate = () => {
        if (process.platform === "win32" && child.pid)
            spawnSync("taskkill", ["/PID", String(child.pid), "/T", "/F"], {
                windowsHide: true,
                stdio: "ignore",
            });
        else child.kill("SIGTERM");
    };
    const timer = setTimeout(() => {
        timedOut = true;
        terminate();
    }, timeoutMinutes * 60_000);
    const accept = (line) => {
        if (!line.trim()) return;
        try {
            const event = JSON.parse(line);
            const row = summarizeEvent(event, rows.length + 1);
            rows.push(row);
            if (row.usage) record.usage = row.usage;
            if (event.model) record.settings.actualModel = event.model;
            if (row.error) failure = row.error;
            writeFileSync(eventsPath, `${JSON.stringify(row)}\n`, {
                flag: "a",
            });
        } catch {
            /* Non-JSON progress is not a trusted agent event. */
        }
    };
    child.stdout.on("data", (chunk) => {
        outputBytes += chunk.length;
        if (outputBytes > 32 * 1024 * 1024) {
            failure = "Trial output limit exceeded";
            terminate();
            return;
        }
        pending += chunk.toString("utf8");
        const lines = pending.split("\n");
        pending = lines.pop();
        lines.forEach(accept);
    });
    child.stderr.on("data", (chunk) => {
        const text = chunk.toString("utf8");
        const model = /^model:\s*(\S+)/mu.exec(text);
        if (model) record.settings.actualModel = model[1];
        const lines = errorLines(text, 2).filter(
            (line) =>
                line.length < 1000 &&
                !/instructions_variables|policy_template|persistent_instructions/u.test(
                    line,
                ),
        );
        if (lines.length) record.launchDiagnostics = lines;
    });
    const interrupted = () => {
        failure = "Trial interrupted";
        terminate();
    };
    process.once("SIGINT", interrupted);
    process.once("SIGTERM", interrupted);
    child.stdin.end(prompt);
    try {
        record.exitCode = await new Promise((resolveExit) => {
            child.once("error", (error) => {
                failure = redactDiagnostic(error.message);
                resolveExit(null);
            });
            child.once("close", (code) => resolveExit(code));
        });
        accept(pending);
        const probePath = join(prepared.candidate, "agent-write-probe.txt");
        record.environmentWritable =
            existsSync(probePath) &&
            readFileSync(probePath, "utf8").trim() === probeValue;
        let grade;
        try {
            grade = gradeCase(id, prepared.candidate);
        } catch (error) {
            grade = { passed: false, error: redactDiagnostic(error.message) };
        }
        record.grade = grade;
        record.status = timedOut
            ? "timed-out"
            : failure
              ? "failed"
              : record.exitCode === 0
                ? "completed"
                : "failed";
        record.passed =
            record.status === "completed" &&
            grade.passed &&
            record.environmentWritable;
        if (!record.environmentWritable && !grade.passed)
            record.failureCategory = "execution-environment-unverified";
        record.failure = failure;
        record.durationMs = Math.round(performance.now() - started);
        record.toolCalls = rows.filter(
            (row) =>
                row.event === "item.completed" &&
                row.itemType === "command_execution",
        ).length;
        record.failedToolCalls = rows.filter(
            (row) =>
                row.event === "item.completed" &&
                typeof row.exitCode === "number" &&
                row.exitCode !== 0,
        ).length;
        record.finishedAtUtc = new Date().toISOString();
        save(trialPath, record);
        return record;
    } finally {
        clearTimeout(timer);
        process.removeListener("SIGINT", interrupted);
        process.removeListener("SIGTERM", interrupted);
    }
}

export function trialRecords() {
    const root = join(repositoryRoot, ".artifacts/coding-evals");
    return existsSync(root)
        ? readdirSync(root).flatMap((name) => {
              const path = join(root, name, "trial.json");
              return existsSync(path)
                  ? [
                        {
                            ...JSON.parse(readFileSync(path, "utf8")),
                            recordPath: path,
                        },
                    ]
                  : [];
          })
        : [];
}

export function compareTrials(records = trialRecords()) {
    return compareTrialRecords(records);
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    const [action, id, variant = "control"] = process.argv.slice(2);
    try {
        let result;
        if (action === "suite") {
            const cases = JSON.parse(
                readFileSync(
                    join(
                        repositoryRoot,
                        "Tooling/ai-development/coding-evals/cases.json",
                    ),
                    "utf8",
                ),
            ).cases;
            result = { schemaVersion: 1, trials: [] };
            for (const entry of cases)
                for (const instructionVariant of ["control", "api-skill"]) {
                    const trial = await runTrial(entry.id, {
                        variant: instructionVariant,
                    });
                    result.trials.push({
                        id: trial.id,
                        variant: trial.variant,
                        passed: trial.passed,
                        status: trial.status,
                        candidate: trial.candidate,
                    });
                    console.log(JSON.stringify(result.trials.at(-1)));
                    save(
                        join(
                            repositoryRoot,
                            ".artifacts/coding-evals/suite.json",
                        ),
                        result,
                    );
                }
        } else
            result =
                action === "run"
                    ? await runTrial(id, { variant })
                    : action === "compare"
                      ? compareTrials()
                      : null;
        if (!result)
            throw new Error("Use run <case> [control|api-skill] or compare");
        console.log(JSON.stringify(result, null, 2));
        if (action === "run" && !result.passed) process.exitCode = 1;
    } catch (error) {
        console.error(redactDiagnostic(error.message));
        process.exitCode = 1;
    }
}
