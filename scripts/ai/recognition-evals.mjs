import { createHash } from "node:crypto";
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { join, resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { repositoryRoot } from "./generate-feature.mjs";

const units = {
    g: ["g", "gram", "grams", "г"],
    ml: ["ml", "мл"],
    pcs: ["pcs", "pc", "piece", "pieces", "шт"],
};
const normalized = (value) =>
    typeof value === "string"
        ? value.trim().normalize("NFC").toLowerCase()
        : "";
const equalNumber = (expected, actual) =>
    expected === null
        ? actual === null
        : typeof actual === "number" &&
          Number.isFinite(actual) &&
          Math.abs(expected - actual) <= 0.001;
const percentile = (values, fraction) =>
    values.length
        ? values.toSorted((a, b) => a - b)[
              Math.ceil(values.length * fraction) - 1
          ]
        : null;
export const corpusHash = (corpus) =>
    createHash("sha256").update(JSON.stringify(corpus)).digest("hex");

export function gradeRecognition(corpus, candidate) {
    if (
        candidate.schemaVersion !== 1 ||
        candidate.corpusSha256 !== corpusHash(corpus) ||
        !Array.isArray(candidate.replies)
    )
        throw new Error("Recording does not match the reference corpus");
    const ids = candidate.replies.map((reply) => reply.caseId);
    if (
        new Set(ids).size !== ids.length ||
        ids.some((id) => !corpus.cases.some((item) => item.id === id))
    )
        throw new Error("Duplicate or unknown recorded case");
    const cases = corpus.cases.map((item) => {
        const recording = candidate.replies.find(
            (reply) => reply.caseId === item.id,
        );
        const errors = [];
        const response = recording?.response;
        if (!response || typeof response !== "object")
            errors.push("missing-response");
        else if (item.kind === "label") {
            const label = response.productLabel ?? response;
            for (const [field, value] of Object.entries(item.expected)) {
                if (
                    typeof value === "string"
                        ? normalized(value) !== normalized(label[field])
                        : !equalNumber(value, label[field])
                )
                    errors.push(`label-${field}`);
            }
        } else {
            if (
                !Array.isArray(response.items) ||
                response.items.length !== item.expected.items.length
            )
                errors.push("food-count");
            else {
                const remaining = [...response.items];
                for (const expected of item.expected.items) {
                    const index = remaining.findIndex((actual) =>
                        expected.aliases.includes(normalized(actual?.nameEn)),
                    );
                    const actual =
                        index < 0 ? null : remaining.splice(index, 1)[0];
                    if (!actual) {
                        errors.push("food-identity");
                        continue;
                    }
                    if (!equalNumber(expected.amount, actual.amount))
                        errors.push("food-quantity");
                    if (!units[expected.unit].includes(normalized(actual.unit)))
                        errors.push("food-unit");
                    if (
                        typeof actual.confidence !== "number" ||
                        !Number.isFinite(actual.confidence) ||
                        actual.confidence < 0 ||
                        actual.confidence > 1
                    )
                        errors.push("food-confidence");
                }
            }
        }
        return {
            id: item.id,
            kind: item.kind,
            passed: errors.length === 0,
            errors,
            latencyMs:
                typeof recording?.latencyMs === "number" &&
                Number.isFinite(recording.latencyMs) &&
                recording.latencyMs >= 0
                    ? recording.latencyMs
                    : null,
            reportedCostUsd:
                typeof recording?.costUsd === "number" &&
                Number.isFinite(recording.costUsd) &&
                recording.costUsd >= 0
                    ? recording.costUsd
                    : null,
        };
    });
    const latencies = cases
        .map((item) => item.latencyMs)
        .filter((value) => value !== null);
    const costs = cases
        .map((item) => item.reportedCostUsd)
        .filter((value) => value !== null);
    return {
        schemaVersion: 1,
        evidenceKind:
            "imported-recorded-replies; provider metadata not independently verified",
        providerCallsMadeByGrader: 0,
        corpusSha256: corpusHash(corpus),
        corpusVersion: corpus.version,
        coverage: corpus.coverage,
        limitations: corpus.limitations,
        model: candidate.model ?? null,
        promptRevision: candidate.promptRevision ?? null,
        passed: cases.filter((item) => item.passed).length,
        total: cases.length,
        exactCaseAccuracy:
            cases.filter((item) => item.passed).length / cases.length,
        p50LatencyMs: percentile(latencies, 0.5),
        p95LatencyMs: percentile(latencies, 0.95),
        latencyCoverage: latencies.length,
        reportedCostCoverage: costs.length,
        totalReportedCostUsd:
            costs.length === cases.length
                ? costs.reduce((sum, cost) => sum + cost, 0)
                : null,
        cases,
    };
}

export function referenceReplies(corpus) {
    return {
        schemaVersion: 1,
        corpusSha256: corpusHash(corpus),
        model: "grader-self-test-oracle",
        promptRevision: "synthetic-reference",
        replies: corpus.cases.map((item) => ({
            caseId: item.id,
            response:
                item.kind === "label"
                    ? { ...item.expected, notes: null }
                    : {
                          items: item.expected.items.map(
                              ({ aliases: _, ...value }) => ({
                                  ...value,
                                  nameLocal: null,
                                  confidence: 1,
                              }),
                          ),
                      },
        })),
    };
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    const [action, input] = process.argv.slice(2);
    const corpus = JSON.parse(
        readFileSync(
            join(
                repositoryRoot,
                "Tooling/ai-development/recognition-evals/cases.json",
            ),
            "utf8",
        ),
    );
    const directory = join(
        repositoryRoot,
        ".artifacts/reliability/recognition",
    );
    mkdirSync(directory, { recursive: true });
    if (action === "prepare") {
        writeFileSync(
            join(directory, "recording-template.json"),
            JSON.stringify(
                {
                    ...referenceReplies(corpus),
                    model: null,
                    promptRevision: null,
                    replies: corpus.cases.map((item) => ({
                        caseId: item.id,
                        response: null,
                    })),
                },
                null,
                2,
            ),
        );
        console.log(
            JSON.stringify({
                cases: corpus.cases.length,
                corpusSha256: corpusHash(corpus),
                coverage: corpus.coverage,
                providerCalls: 0,
            }),
        );
    } else if (action === "self-test") {
        const report = gradeRecognition(corpus, referenceReplies(corpus));
        if (report.passed !== report.total)
            throw new Error(
                "Reference grader rejected its independently authored corpus",
            );
        report.evidenceKind = "grader-self-test; no model-quality evidence";
        writeFileSync(
            join(directory, "grader-self-test.json"),
            `${JSON.stringify(report, null, 2)}\n`,
        );
        console.log(
            JSON.stringify({
                status: "passed",
                cases: report.total,
                providerCalls: 0,
                modelQualityMeasured: false,
            }),
        );
    } else if (action === "grade" && input) {
        let candidate;
        try {
            candidate = JSON.parse(readFileSync(resolve(input), "utf8"));
        } catch {
            throw new Error("Recorded replies could not be read as JSON");
        }
        const report = gradeRecognition(corpus, candidate);
        writeFileSync(
            join(directory, "recorded-quality.json"),
            `${JSON.stringify(report, null, 2)}\n`,
        );
        console.log(
            JSON.stringify({
                passed: report.passed,
                total: report.total,
                evidenceKind: report.evidenceKind,
            }),
        );
    } else throw new Error("Use prepare, self-test or grade <recorded-json>");
}
