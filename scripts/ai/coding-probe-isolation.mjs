import { randomUUID } from "node:crypto";
import { spawnSync } from "node:child_process";
import { realpathSync } from "node:fs";
import { join } from "node:path";
import { containedPath, repositoryRoot } from "./generate-feature.mjs";
import { redactDiagnostic } from "./diagnostic-redaction.mjs";

const image =
    "node:24-bookworm-slim@sha256:d6aa754f16b3197301076f047b5def2f02ea1dbbc2ca920407d46d7ec7f87b20";

function runProbeContainer(mounts, command) {
    const owner = randomUUID();
    const name = `fd-coding-probe-${owner}`;
    try {
        return spawnSync(
            "docker",
            [
                "run",
                "--rm",
                "--name",
                name,
                "--label",
                `fooddiary.coding-probe=${owner}`,
                "--network",
                "none",
                "--cap-drop",
                "ALL",
                "--security-opt",
                "no-new-privileges",
                "--read-only",
                "--pids-limit",
                "32",
                "--memory",
                "256m",
                "--cpus",
                "1",
                "--user",
                "65534:65534",
                ...mounts,
                image,
                ...command,
            ],
            {
                windowsHide: true,
                encoding: "utf8",
                timeout: 30_000,
                maxBuffer: 1024 * 1024,
            },
        );
    } finally {
        const inspect = spawnSync(
            "docker",
            [
                "inspect",
                "--format",
                '{{index .Config.Labels "fooddiary.coding-probe"}}',
                name,
            ],
            { windowsHide: true, encoding: "utf8", timeout: 10_000 },
        );
        if (inspect.status === 0 && inspect.stdout.trim() === owner) {
            const removed = spawnSync("docker", ["rm", "-f", name], {
                windowsHide: true,
                encoding: "utf8",
                timeout: 10_000,
            });
            if (removed.status !== 0)
                throw new Error(
                    "Owned coding probe container could not be cleaned up",
                );
        }
    }
}

export function checkCodingProbeEnvironment() {
    const marker = "fooddiary-coding-probe-ready";
    const result = runProbeContainer(
        [],
        ["node", "-e", `process.stdout.write(${JSON.stringify(marker)})`],
    );
    if (result.status !== 0 || result.error || result.stdout !== marker)
        throw new Error(
            `Coding probe environment is unavailable: ${redactDiagnostic(
                result.error?.message ||
                    result.stderr ||
                    "trusted container check failed",
            )}`,
        );
}

export function isolatedRuntimeProbes(id, candidate) {
    if (!/^[a-z0-9-]{1,60}$/u.test(id))
        throw new Error("Invalid coding probe identity");
    const source = containedPath(candidate, "FoodDiary.Web.Client/src");
    const dependencies = realpathSync(
        join(repositoryRoot, "FoodDiary.Web.Client/node_modules"),
    );
    const worker = join(repositoryRoot, "scripts/ai/coding-probe-worker.cjs");
    if ([source, dependencies, worker].some((path) => path.includes(",")))
        throw new Error("Container bind paths cannot contain commas");
    const result = runProbeContainer(
        [
            "--mount",
            `type=bind,source=${source},target=/candidate/FoodDiary.Web.Client/src,readonly`,
            "--mount",
            `type=bind,source=${dependencies},target=/dependencies/node_modules,readonly`,
            "--mount",
            `type=bind,source=${worker},target=/worker.cjs,readonly`,
        ],
        ["node", "/worker.cjs", id],
    );
    let outcome;
    try {
        outcome = JSON.parse(result.stdout);
    } catch {
        throw new Error(
            `Isolated probe returned no valid result: ${redactDiagnostic(result.error?.message || result.stderr || "")}`,
        );
    }
    if (result.status !== 0 || outcome.passed !== true || outcome.id !== id)
        throw new Error(
            redactDiagnostic(
                outcome.error ??
                    result.error?.message ??
                    "Isolated runtime probe failed",
            ),
        );
    return {
        image,
        network: "disabled",
        filesystem: "read-only scoped mounts",
        passed: true,
    };
}

export function evaluateRuntimeProbes(
    id,
    candidate,
    {
        probe = isolatedRuntimeProbes,
        checkEnvironment = checkCodingProbeEnvironment,
    } = {},
) {
    try {
        checkEnvironment();
    } catch (error) {
        return {
            runtimeError: null,
            environmentError: redactDiagnostic(error.message),
        };
    }
    try {
        probe(id, candidate);
        return { runtimeError: null, environmentError: null };
    } catch (error) {
        let environmentError = null;
        try {
            checkEnvironment();
        } catch (environmentFailure) {
            environmentError = redactDiagnostic(environmentFailure.message);
        }
        return {
            runtimeError: redactDiagnostic(error.message),
            environmentError,
        };
    }
}
