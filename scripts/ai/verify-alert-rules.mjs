import { spawnSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";

const image =
    "prom/prometheus@sha256:63805ebb8d2b3920190daf1cb14a60871b16fd38bed42b857a3182bc621f4996";
const mount = `${join(repositoryRoot, "infra/observability/prometheus")}:/rules:ro`;
const checks = [
    [
        "check",
        "rules",
        "fooddiary-backend-alerts.yml",
        "fooddiary-slo-alerts.yml",
    ],
    ["test", "rules", "fooddiary-alert-tests.yml"],
];
const results = [];
for (const check of checks) {
    const started = performance.now();
    const result = spawnSync(
        "docker",
        [
            "run",
            "--rm",
            "--network=none",
            "--memory=256m",
            "--cpus=1",
            "-v",
            mount,
            "-w",
            "/rules",
            "--entrypoint",
            "/bin/promtool",
            image,
            ...check,
        ],
        {
            encoding: "utf8",
            windowsHide: true,
            timeout: 120_000,
            maxBuffer: 1024 * 1024,
        },
    );
    results.push({
        command: check.join(" "),
        passed: !result.error && result.status === 0,
        seconds: (performance.now() - started) / 1000,
    });
    console.log(result.stdout ?? "");
    if (result.status !== 0)
        console.error(result.stderr ?? "Alert rule validation failed");
}
const directory = join(repositoryRoot, ".artifacts/reliability");
mkdirSync(directory, { recursive: true });
writeFileSync(
    join(directory, "alerts.json"),
    JSON.stringify(
        {
            schemaVersion: 1,
            image,
            scope: "deterministic local rule evaluation; production loading, scraping and notification delivery unverified",
            scenarios: 7,
            results,
        },
        null,
        2,
    ),
);
if (results.some((result) => !result.passed)) process.exitCode = 1;
