import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";

export function trialSourceFingerprints(root = repositoryRoot) {
    const shared = ["generate-feature.mjs", "diagnostic-redaction.mjs"];
    const fingerprint = (names) => {
        const sources = [...shared, ...names].sort().map((name) => {
            const path = `scripts/ai/${name}`;
            return {
                path,
                sha256: createHash("sha256")
                    .update(readFileSync(join(root, path)))
                    .digest("hex"),
            };
        });
        return {
            sources,
            hash: createHash("sha256")
                .update(JSON.stringify(sources))
                .digest("hex"),
        };
    };
    const harness = fingerprint([
        "agent-trials.mjs",
        "trial-settings.mjs",
        "trial-fingerprints.mjs",
        "candidate-contract-check.cjs",
    ]);
    const grader = fingerprint([
        "coding-evals.mjs",
        "coding-probe-isolation.mjs",
        "coding-probe-worker.cjs",
    ]);
    return {
        harnessHash: harness.hash,
        graderEngineHash: grader.hash,
        sourceFingerprints: {
            harness: harness.sources,
            grader: grader.sources,
        },
    };
}
