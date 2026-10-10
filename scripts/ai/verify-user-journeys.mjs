import { randomUUID } from "node:crypto";
import { spawn } from "node:child_process";
import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { createInterface } from "node:readline";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";
import { startRuntime, stopRuntime } from "./task-runtime.mjs";
import { runtimeDiagnosticState } from "./runtime-diagnostics.mjs";
import { ownedRuntime } from "./owned-runtime-client.mjs";
import { acquireRuntimeCheck } from "./runtime-check-lease.mjs";
import {
    safeJourneyOutput,
    sanitizeJourneyReport,
} from "./journey-evidence.mjs";

const supplied = process.argv.slice(2);
if (supplied.length && (supplied.length !== 2 || supplied[0] !== "--runtime"))
    throw new Error(
        "Use --runtime <owned-name>, or omit to start and stop a fresh runtime",
    );
const name = supplied[1] ?? `journey-${randomUUID().slice(0, 8)}`;
const created = !supplied[1];
const runId = randomUUID();
let started = false;
let fixtureSecrets = [];
let releaseLease;
try {
    const typecheck = spawn(
        process.execPath,
        [
            join(
                repositoryRoot,
                "FoodDiary.Web.Client/node_modules/typescript/bin/tsc",
            ),
            "-p",
            "e2e/client-live/tsconfig.json",
        ],
        {
            cwd: join(repositoryRoot, "FoodDiary.Web.Client"),
            windowsHide: true,
            stdio: ["ignore", "pipe", "pipe"],
        },
    );
    for (const output of [typecheck.stdout, typecheck.stderr]) {
        const lines = createInterface({ input: output, crlfDelay: Infinity });
        lines.on("line", (line) =>
            process.stdout.write(`${safeJourneyOutput(line)}\n`),
        );
    }
    const typeStatus = await new Promise((resolveExit, reject) => {
        typecheck.once("error", reject);
        typecheck.once("close", resolveExit);
    });
    if (typeStatus !== 0) throw new Error("Live fixture type checking failed");
    if (created) {
        await startRuntime(name);
        started = true;
    }
    const state = runtimeDiagnosticState(name);
    releaseLease = acquireRuntimeCheck(ownedRuntime(name), "browser journeys");
    const privateCredentials = JSON.parse(
        readFileSync(
            join(
                repositoryRoot,
                `.artifacts/task-runtime/${name}/credentials.json`,
            ),
            "utf8",
        ),
    );
    fixtureSecrets = [privateCredentials.email, privateCredentials.password];
    if (
        state.status !== "ready" ||
        state.staleSources ||
        state.processes.some((process) => !process.alive)
    )
        throw new Error("Owned runtime is unavailable or stale");
    const child = spawn(
        process.execPath,
        [
            join(
                repositoryRoot,
                "FoodDiary.Web.Client/node_modules/@playwright/test/cli.js",
            ),
            "test",
            "-c",
            "playwright.client.live.config.ts",
        ],
        {
            cwd: join(repositoryRoot, "FoodDiary.Web.Client"),
            windowsHide: true,
            stdio: ["ignore", "pipe", "pipe"],
            env: {
                ...process.env,
                FOODDIARY_E2E_RUNTIME: name,
                FOODDIARY_E2E_RUN_ID: runId,
            },
        },
    );
    for (const output of [child.stdout, child.stderr]) {
        const lines = createInterface({ input: output, crlfDelay: Infinity });
        lines.on("line", (line) =>
            process.stdout.write(
                `${safeJourneyOutput(line, fixtureSecrets)}\n`,
            ),
        );
    }
    process.exitCode = await new Promise((resolveExit, reject) => {
        child.once("error", reject);
        child.once("close", (code) => resolveExit(code ?? 1));
    });
} finally {
    const report = join(
        repositoryRoot,
        `.artifacts/reliability/${name}/journeys-${runId}/results.json`,
    );
    try {
        if (existsSync(report)) {
            try {
                writeFileSync(
                    report,
                    `${JSON.stringify(sanitizeJourneyReport(JSON.parse(readFileSync(report, "utf8")), fixtureSecrets), null, 2)}\n`,
                );
            } catch {
                writeFileSync(
                    report,
                    JSON.stringify({
                        status: "unverified",
                        error: "Live report could not be safely parsed; raw output was replaced",
                    }),
                );
                process.exitCode = 1;
            }
        }
    } finally {
        releaseLease?.();
        if (started) stopRuntime(name);
    }
}
