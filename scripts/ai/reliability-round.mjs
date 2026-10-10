import { randomUUID } from "node:crypto";
import { spawn } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { createInterface } from "node:readline";
import { repositoryRoot } from "./generate-feature.mjs";
import { startRuntime, stopRuntime } from "./task-runtime.mjs";
import { cleanStoppedBuild } from "./resource-budget.mjs";
import { safeJourneyOutput } from "./journey-evidence.mjs";

const name = `reliability-${randomUUID().slice(0, 8)}`;
const directory = join(repositoryRoot, ".artifacts/reliability", name);
mkdirSync(directory, { recursive: true });
const report = {
    schemaVersion: 1,
    runtime: name,
    scope: "owned local integration and workstation baselines",
    steps: [],
    externalModelQualityMeasured: false,
    productionVerified: false,
};
let started = false;
let secrets = [];
async function run(label, args) {
    const begin = performance.now();
    const child = spawn(process.execPath, args, {
        cwd: repositoryRoot,
        windowsHide: true,
        stdio: ["ignore", "pipe", "pipe"],
    });
    for (const stream of [child.stdout, child.stderr])
        createInterface({ input: stream, crlfDelay: Infinity }).on(
            "line",
            (line) => console.log(safeJourneyOutput(line, secrets)),
        );
    const code = await new Promise((resolveExit, reject) => {
        child.once("error", reject);
        child.once("close", resolveExit);
    });
    report.steps.push({
        label,
        passed: code === 0,
        seconds: (performance.now() - begin) / 1000,
    });
}
try {
    await run("recognition-grader", [
        "scripts/ai/recognition-evals.mjs",
        "self-test",
    ]);
    await run("alert-rules", ["scripts/ai/verify-alert-rules.mjs"]);
    await startRuntime(name);
    started = true;
    const credentials = JSON.parse(
        readFileSync(
            join(
                repositoryRoot,
                ".artifacts/task-runtime",
                name,
                "credentials.json",
            ),
            "utf8",
        ),
    );
    secrets = [credentials.email, credentials.password];
    await run("load-baseline", ["scripts/ai/load-baseline.mjs", name]);
    await run("database-recovery", ["scripts/ai/recovery-drill.mjs", name]);
    await run("persisted-user-journeys", [
        "scripts/ai/verify-user-journeys.mjs",
        "--runtime",
        name,
    ]);
} catch (error) {
    report.error = safeJourneyOutput(error.message, secrets);
} finally {
    if (started) {
        try {
            const stopped = stopRuntime(name);
            cleanStoppedBuild(stopped);
            report.ownedRuntimeStopped = true;
            report.ownedBuildCleaned = true;
        } catch (error) {
            report.cleanupError = safeJourneyOutput(error.message, secrets);
        }
    }
    report.status =
        !report.error &&
        !report.cleanupError &&
        report.steps.length === 5 &&
        report.steps.every((step) => step.passed)
            ? "passed"
            : "failed";
    writeFileSync(
        join(directory, "summary.json"),
        `${JSON.stringify(report, null, 2)}\n`,
    );
    console.log(JSON.stringify(report));
    if (report.status !== "passed") process.exitCode = 1;
}
