import { createHash, randomUUID } from "node:crypto";
import { spawn, spawnSync } from "node:child_process";
import {
    existsSync,
    mkdirSync,
    readFileSync,
    writeFileSync,
    openSync,
    closeSync,
    unlinkSync,
    renameSync,
    readdirSync,
    readSync,
    fstatSync,
} from "node:fs";
import { dirname, join, resolve, relative } from "node:path";
import { pathToFileURL, fileURLToPath } from "node:url";
import { repositoryRoot, containedPath } from "./generate-feature.mjs";
import { processIdentity } from "./task-runtime.mjs";
import { errorLines, redactDiagnostic } from "./diagnostic-redaction.mjs";

const digest = (value) => createHash("sha256").update(value).digest("hex");
const save = (path, value) => {
    const temporary = `${path}.${process.pid}.tmp`;
    writeFileSync(temporary, `${JSON.stringify(value, null, 2)}\n`);
    renameSync(temporary, path);
};
const git = (args) => {
    const result = spawnSync("git", args, {
        cwd: repositoryRoot,
        encoding: "utf8",
        windowsHide: true,
        maxBuffer: 32 * 1024 * 1024,
    });
    if (result.status !== 0)
        throw new Error("Cannot resolve repository snapshot");
    return result.stdout;
};
function stopCheckProcess(owner) {
    const current = processIdentity(owner.pid);
    if (current === null) return;
    if (current !== owner.identity)
        throw new Error("Refusing to stop a reused verification PID");
    if (process.platform === "win32") {
        const result = spawnSync(
            "taskkill",
            ["/PID", String(owner.pid), "/T", "/F"],
            { windowsHide: true, encoding: "utf8" },
        );
        if (result.status !== 0 && processIdentity(owner.pid) !== null)
            throw new Error("Owned verification process could not be stopped");
    } else {
        // The worker starts this check in its own process group on POSIX.
        process.kill(-owner.pid, "SIGKILL");
    }
}
export function taskFingerprint() {
    const hash = createHash("sha256");
    hash.update(git(["rev-parse", "HEAD"]));
    hash.update(git(["diff", "--binary", "HEAD"]));
    for (const path of git(["ls-files", "--others", "--exclude-standard", "-z"])
        .split("\0")
        .filter(Boolean)
        .sort()) {
        hash.update(path);
        hash.update(readFileSync(containedPath(repositoryRoot, path)));
    }
    return hash.digest("hex");
}

export function evidenceFile(path) {
    if (
        typeof path !== "string" ||
        !/^\.artifacts\/llm-wiki\/(?:[A-Za-z0-9_-]+\.json|tasks\/[A-Za-z0-9_-]+\/evidence\.json)$/u.test(
            path,
        )
    )
        throw new Error("Choose an existing task-owned Wiki evidence bundle");
    const absolute = containedPath(repositoryRoot, path);
    if (!existsSync(absolute))
        throw new Error(
            "Evidence bundle is unavailable; initialize it through Wiki first",
        );
    const evidence = JSON.parse(readFileSync(absolute, "utf8"));
    if (
        !Array.isArray(evidence.checks) ||
        !Array.isArray(evidence.change?.changedPaths) ||
        !/^[a-f0-9]{40}$/u.test(evidence.git?.base ?? "")
    )
        throw new Error(
            "Evidence bundle has no canonical check scope and immutable baseline",
        );
    return { absolute, evidence };
}

function jobDirectory(id) {
    if (typeof id !== "string" || !/^[a-f0-9-]{36}$/u.test(id))
        throw new Error("Invalid verification job identity");
    return containedPath(repositoryRoot, `.artifacts/agent-checks/${id}`);
}

export function readJob(id) {
    const path = join(jobDirectory(id), "job.json");
    const record = JSON.parse(readFileSync(path, "utf8"));
    if (
        record.id !== id ||
        resolve(record.checkout) !== resolve(repositoryRoot)
    )
        throw new Error("Verification belongs to another checkout");
    return { path, record };
}

function recentJobs(evidencePath) {
    const root = join(repositoryRoot, ".artifacts/agent-checks");
    if (!existsSync(root)) return [];
    return readdirSync(root)
        .filter((id) => /^[a-f0-9-]{36}$/u.test(id))
        .flatMap((id) => {
            const path = join(root, id, "job.json");
            if (!existsSync(path)) return [];
            const job = readJob(id).record;
            return job.evidencePath === evidencePath ? [job] : [];
        })
        .sort((left, right) =>
            right.startedAtUtc.localeCompare(left.startedAtUtc),
        );
}

function observedWorker(record) {
    const launcher = join(jobDirectory(record.id), "launcher.json");
    const owner =
        record.worker ??
        (existsSync(launcher)
            ? JSON.parse(readFileSync(launcher, "utf8"))
            : null);
    if (!owner) return { alive: false, state: "unverified" };
    const current = processIdentity(owner.pid);
    if (current === null) return { alive: false, state: "interrupted" };
    if (owner.identity === null || owner.identity === undefined)
        return { alive: false, state: "unverified" };
    return {
        alive: current === owner.identity,
        state: current === owner.identity ? record.status : "interrupted",
    };
}

export function taskDiagnostics(evidencePath, jobId) {
    const result = {
        schemaVersion: 1,
        checkout: repositoryRoot,
        sourceFingerprint: taskFingerprint(),
    };
    if (evidencePath) {
        const { evidence } = evidenceFile(evidencePath);
        result.checks = evidence.checks.map((check) => ({
            id: check.id,
            status: check.status,
            reason: redactDiagnostic(check.reason ?? ""),
        }));
        result.pending = result.checks.filter(
            (check) =>
                ![
                    "passed",
                    "passed-with-known-baseline-failures",
                    "not-applicable",
                ].includes(check.status),
        );
        result.jobs = recentJobs(evidencePath)
            .slice(0, 5)
            .map((job) => ({
                id: job.id,
                checkId: job.checkId,
                status: ["queued", "running"].includes(job.status)
                    ? observedWorker(job).state
                    : job.status,
                stale: job.sourceFingerprint !== result.sourceFingerprint,
            }));
    }
    if (jobId) {
        const { record } = readJob(jobId);
        const observation = observedWorker(record);
        const log = containedPath(
            repositoryRoot,
            `.artifacts/agent-checks/${jobId}/check.log`,
        );
        result.job = {
            ...record,
            stale: record.sourceFingerprint !== result.sourceFingerprint,
            workerAlive: observation.alive,
            errors: existsSync(log) ? errorLines(readTail(log), 15) : [],
        };
        if (
            ["queued", "running"].includes(record.status) &&
            existsSync(join(jobDirectory(jobId), "cancel.flag"))
        )
            result.job.status = "cancel-requested";
        if (
            ["queued", "running", "cancel-requested"].includes(record.status) &&
            !result.job.workerAlive &&
            (record.worker ||
                Date.now() - Date.parse(record.startedAtUtc) > 30_000)
        )
            result.job.status = observation.state;
    }
    return result;
}

function readTail(path) {
    const descriptor = openSync(path, "r");
    try {
        const length = Math.min(fstatSync(descriptor).size, 32_000);
        const buffer = Buffer.alloc(length);
        readSync(
            descriptor,
            buffer,
            0,
            length,
            Math.max(0, fstatSync(descriptor).size - length),
        );
        return buffer.toString("utf8");
    } finally {
        closeSync(descriptor);
    }
}

export function nextTaskAction(evidencePath) {
    evidenceFile(evidencePath);
    const diagnostics = taskDiagnostics(evidencePath);
    const active = diagnostics.jobs.find((job) =>
        ["queued", "running", "cancel-requested"].includes(job.status),
    );
    if (active)
        return {
            state: "verification-running",
            nextAction: "get_task_diagnostics",
            jobId: active.id,
            evidencePath,
        };
    const latest = diagnostics.jobs[0];
    if (latest && ["unverified", "interrupted"].includes(latest.status))
        return {
            state: "attention-required",
            nextAction:
                "Inspect get_task_diagnostics and reconcile the owned job with cancel_task_check before starting another check",
            jobId: latest.id,
            evidencePath,
        };
    const staleCheck = latest?.stale
        ? diagnostics.checks.find((check) => check.id === latest.checkId)
        : null;
    const next = staleCheck ?? diagnostics.pending[0];
    return next
        ? {
              state: staleCheck
                  ? "stale-evidence"
                  : next.status === "failed"
                    ? "repair-required"
                    : "verification-required",
              nextAction:
                  next.status === "failed"
                      ? "Inspect get_task_diagnostics errors, fix the owning code, then verify_task"
                      : "verify_task",
              evidencePath,
              checkId: next.id,
              sourceFingerprint: diagnostics.sourceFingerprint,
          }
        : {
              state: "checks-resolved",
              nextAction:
                  "Run Wiki evidence-validate and resolve reviews/acceptance before completion",
              evidencePath,
              sourceFingerprint: diagnostics.sourceFingerprint,
          };
}

export function startCheck(evidencePath, checkId, timeoutMinutes = 20) {
    const { evidence } = evidenceFile(evidencePath);
    if (
        !/^[A-Za-z0-9_-]{1,80}$/u.test(checkId ?? "") ||
        !evidence.checks.some((check) => check.id === checkId)
    )
        throw new Error("Choose a check ID from this evidence plan");
    if (
        !Number.isInteger(timeoutMinutes) ||
        timeoutMinutes < 1 ||
        timeoutMinutes > 30
    )
        throw new Error("Check timeout must be 1-30 minutes");
    const id = randomUUID();
    const directory = jobDirectory(id);
    mkdirSync(directory, { recursive: true });
    const lockPath = containedPath(
        repositoryRoot,
        `.artifacts/agent-checks/${digest(evidencePath)}.lock`,
    );
    let lock;
    try {
        lock = openSync(lockPath, "wx");
    } catch {
        throw new Error(
            "A check for this evidence bundle already owns the execution lock; inspect its job before recovery",
        );
    }
    writeFileSync(lock, id);
    closeSync(lock);
    let record;
    try {
        record = {
            schemaVersion: 1,
            id,
            checkout: repositoryRoot,
            evidencePath,
            checkId,
            timeoutMinutes,
            sourceFingerprint: taskFingerprint(),
            startedAtUtc: new Date().toISOString(),
            status: "queued",
            worker: null,
        };
        save(join(directory, "job.json"), record);
        const workerLog = openSync(join(directory, "worker.log"), "a");
        const child = spawn(
            process.execPath,
            [fileURLToPath(import.meta.url), "worker", id],
            {
                cwd: repositoryRoot,
                detached: true,
                windowsHide: true,
                stdio: ["ignore", workerLog, workerLog],
            },
        );
        closeSync(workerLog);
        if (child.pid)
            save(join(directory, "launcher.json"), {
                pid: child.pid,
                identity: processIdentity(child.pid),
            });
        child.once("error", (error) => {
            record.status = "failed";
            record.error = redactDiagnostic(error.message);
            save(join(directory, "job.json"), record);
            if (existsSync(lockPath) && readFileSync(lockPath, "utf8") === id)
                unlinkSync(lockPath);
        });
        child.unref();
        return { ...record, nextAction: "get_task_diagnostics", jobId: id };
    } catch (error) {
        if (existsSync(lockPath) && readFileSync(lockPath, "utf8") === id)
            unlinkSync(lockPath);
        if (record) {
            record.status = "failed";
            save(join(directory, "job.json"), record);
        }
        throw error;
    }
}

export function cancelCheck(id) {
    const { path, record } = readJob(id);
    if (!["queued", "running"].includes(record.status)) return record;
    const launcherPath = join(dirname(path), "launcher.json");
    const worker =
        record.worker ??
        (existsSync(launcherPath)
            ? JSON.parse(readFileSync(launcherPath, "utf8"))
            : null);
    const currentWorker = worker ? processIdentity(worker.pid) : undefined;
    if (
        worker &&
        (currentWorker === null ||
            (worker.identity != null && currentWorker !== worker.identity))
    ) {
        if (
            record.process &&
            processIdentity(record.process.pid) === record.process.identity
        )
            stopCheckProcess(record.process);
        record.status = "interrupted";
        record.finishedAtUtc = new Date().toISOString();
        save(path, record);
        const lockPath = containedPath(
            repositoryRoot,
            `.artifacts/agent-checks/${digest(record.evidencePath)}.lock`,
        );
        if (existsSync(lockPath) && readFileSync(lockPath, "utf8") === id)
            unlinkSync(lockPath);
        return {
            id,
            status: record.status,
            recovery:
                "Dead owned worker reconciled; evidence remains unverified",
        };
    }
    // A separate marker avoids racing the worker's status writes during startup.
    writeFileSync(join(dirname(path), "cancel.flag"), new Date().toISOString());
    return {
        id,
        status: "cancel-requested",
        nextAction: "get_task_diagnostics",
    };
}

async function checkWorker(id) {
    const { path, record } = readJob(id);
    const directory = dirname(path);
    const lockPath = containedPath(
        repositoryRoot,
        `.artifacts/agent-checks/${digest(record.evidencePath)}.lock`,
    );
    let child;
    let timer;
    let cancelled = false;
    let timedOut = false;
    record.worker = {
        pid: process.pid,
        identity: processIdentity(process.pid),
    };
    try {
        if (existsSync(join(directory, "cancel.flag"))) {
            record.status = "cancelled";
            return;
        }
        record.status = "running";
        save(path, record);
        evidenceFile(record.evidencePath);
        if (taskFingerprint() !== record.sourceFingerprint)
            throw new Error(
                "Sources changed before check start; request a fresh verification",
            );
        const logPath = join(directory, "check.log");
        writeFileSync(logPath, "");
        child = spawn(
            "pwsh",
            [
                "-NoLogo",
                "-NoProfile",
                "-File",
                join(
                    repositoryRoot,
                    ".llm-wiki/tools/Manage-LlmWikiEvidence.ps1",
                ),
                "run",
                "-Path",
                record.evidencePath,
                "-Id",
                record.checkId,
                "-NoExitOnFailure",
            ],
            {
                cwd: repositoryRoot,
                detached: process.platform !== "win32",
                windowsHide: true,
                stdio: ["ignore", "pipe", "pipe"],
            },
        );
        let bytes = 0;
        let pendingOutput = "";
        const output = (chunk) => {
            bytes += chunk.length;
            if (bytes > 8 * 1024 * 1024) {
                record.error = "Check output limit exceeded";
                stopCheckProcess(record.process);
                return;
            }
            pendingOutput += chunk.toString("utf8");
            const lines = pendingOutput.split("\n");
            pendingOutput = lines.pop();
            for (const line of lines)
                writeFileSync(logPath, `${redactDiagnostic(line, 4000)}\n`, {
                    flag: "a",
                });
        };
        child.stdout.on("data", output);
        child.stderr.on("data", output);
        record.process = {
            pid: child.pid,
            identity: processIdentity(child.pid),
        };
        save(path, record);
        timer = setInterval(() => {
            if (existsSync(join(directory, "cancel.flag"))) {
                cancelled = true;
                stopCheckProcess(record.process);
            }
            if (
                Date.now() - Date.parse(record.startedAtUtc) >
                record.timeoutMinutes * 60_000
            ) {
                timedOut = true;
                stopCheckProcess(record.process);
            }
        }, 1000);
        const exitCode = await new Promise((resolveExit, reject) => {
            child.once("error", reject);
            child.once("close", resolveExit);
        });
        record.exitCode = exitCode;
        if (pendingOutput)
            writeFileSync(logPath, redactDiagnostic(pendingOutput, 4000), {
                flag: "a",
            });
        const check = evidenceFile(record.evidencePath).evidence.checks.find(
            (item) => item.id === record.checkId,
        );
        record.recordedCheckStatus = check?.status;
        record.status = cancelled
            ? "cancelled"
            : timedOut
              ? "timed-out"
              : record.error
                ? "failed"
                : taskFingerprint() !== record.sourceFingerprint
                  ? "stale"
                  : exitCode === 0 &&
                      [
                          "passed",
                          "passed-with-known-baseline-failures",
                      ].includes(check?.status)
                    ? "passed"
                    : "failed";
    } catch (error) {
        record.status = "failed";
        record.error = redactDiagnostic(error.message);
    } finally {
        clearInterval(timer);
        record.finishedAtUtc = new Date().toISOString();
        save(path, record);
        if (existsSync(lockPath) && readFileSync(lockPath, "utf8") === id)
            unlinkSync(lockPath);
    }
}

export async function taskTool(request) {
    switch (request.action) {
        case "next":
            return nextTaskAction(request.evidencePath);
        case "diagnostics": {
            const result = taskDiagnostics(request.evidencePath, request.jobId);
            if (request.runtimeName) {
                const { runtimeDiagnosticState } =
                    await import("./runtime-diagnostics.mjs");
                result.runtime = runtimeDiagnosticState(request.runtimeName);
            }
            return result;
        }
        case "collect-runtime": {
            const { collectRuntimeDiagnostics } =
                await import("./runtime-diagnostics.mjs");
            return collectRuntimeDiagnostics(request.runtimeName);
        }
        case "verify":
            return startCheck(
                request.evidencePath,
                request.checkId,
                request.timeoutMinutes ?? 20,
            );
        case "cancel":
            return cancelCheck(request.jobId);
        default:
            throw new Error("Unknown task operation");
    }
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    try {
        if (process.argv[2] === "worker") await checkWorker(process.argv[3]);
        else {
            let input = "";
            for await (const chunk of process.stdin) {
                input += chunk;
                if (input.length > 16_000)
                    throw new Error("Request is too large");
            }
            console.log(JSON.stringify(await taskTool(JSON.parse(input))));
        }
    } catch (error) {
        console.error(redactDiagnostic(error.message));
        process.exitCode = 1;
    }
}
