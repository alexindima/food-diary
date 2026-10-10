import { createHash } from "node:crypto";
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { trialRecords } from "./agent-trials.mjs";
import { containedPath, repositoryRoot } from "./generate-feature.mjs";
import { redactDiagnostic } from "./diagnostic-redaction.mjs";

const sha = (value) => createHash("sha256").update(value).digest("hex");
const outputRoot = join(repositoryRoot, ".artifacts/agent-feedback");
const save = (path, value) => {
    mkdirSync(dirname(path), { recursive: true });
    writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`);
};

export function analyzeTrials(records = trialRecords()) {
    const groups = new Map();
    const add = (theme, row, evidence, recommendation) => {
        const group = groups.get(theme) ?? {
            id: sha(theme).slice(0, 16),
            theme,
            recommendation,
            trials: new Set(),
            evidence: [],
        };
        group.trials.add(row.recordPath ?? row.candidate);
        group.evidence.push({
            trial: row.recordPath,
            trialHash: row.recordPath
                ? sha(readFileSync(row.recordPath))
                : null,
            ...evidence,
        });
        groups.set(theme, group);
    };
    for (const row of records) {
        if (!existsSync(row.eventsPath ?? "")) continue;
        const eventPath = containedPath(repositoryRoot, row.eventsPath);
        const events = readFileSync(eventPath, "utf8")
            .trim()
            .split("\n")
            .filter(Boolean)
            .map((line) => JSON.parse(line));
        const traceHash = sha(readFileSync(eventPath));
        const blocked = events.find((event) =>
            /sandbox_mode.?=.?read-only|apply_patch.*reject|cannot apply.*read.only/iu.test(
                event.message ?? "",
            ),
        );
        if (blocked)
            add(
                "execution-environment-not-writable",
                row,
                {
                    sequence: blocked.sequence,
                    traceHash,
                    message: redactDiagnostic(blocked.message, 900),
                },
                "Verify the actual candidate write permission before a coding trial; record infrastructure failure separately from model capability.",
            );
        if (row.grade && !row.passed && row.grade.changedPaths?.length === 0)
            add(
                "no-production-change",
                row,
                {
                    traceHash,
                    outcome:
                        "External grader failed and candidate production hashes are unchanged",
                },
                "Keep a required production-change outcome rule for historical baselines that are known to fail; agent completion text is not proof.",
            );
        for (const event of events.filter(
            (item) =>
                item.event === "item.completed" &&
                typeof item.exitCode === "number" &&
                item.exitCode !== 0,
        )) {
            add(
                "failed-tool-call",
                row,
                {
                    traceHash,
                    sequence: event.sequence,
                    command: event.command,
                    errors: event.errors,
                },
                "Inspect the exact failed call and distinguish an expected no-match from a broken tool or environment before changing instructions.",
            );
        }
        const commands = new Map();
        for (const event of events.filter(
            (item) => item.event === "item.completed" && item.command,
        )) {
            const key = event.command.replace(/\s+/gu, " ").trim();
            commands.set(key, [...(commands.get(key) ?? []), event.sequence]);
        }
        for (const [command, sequences] of commands)
            if (sequences.length > 1)
                add(
                    "repeated-identical-read",
                    row,
                    { traceHash, command, sequences },
                    "Review repeated reads in context; add a bounded combined tool only when repetition is avoidable.",
                );
    }
    return {
        schemaVersion: 1,
        generatedAtUtc: new Date().toISOString(),
        trials: records.length,
        candidates: [...groups.values()].map((group) => ({
            ...group,
            trials: [...group.trials],
            occurrences: group.trials.size,
            status: "proposed",
        })),
    };
}

export function draftRegression(
    candidateId,
    analysisPath = join(outputRoot, "analysis.json"),
) {
    const analysis = JSON.parse(readFileSync(analysisPath, "utf8"));
    const candidate = analysis.candidates.find(
        (item) => item.id === candidateId,
    );
    if (!candidate) throw new Error("Choose a grounded feedback candidate");
    const rule =
        candidate.theme === "no-production-change"
            ? "production-change-required"
            : candidate.theme === "execution-environment-not-writable"
              ? "writable-environment-required"
              : null;
    if (!rule)
        throw new Error(
            "This signal needs human interpretation; it cannot become a generic mandatory rule",
        );
    const proposal = {
        schemaVersion: 1,
        id: candidate.id,
        rule,
        sourceAnalysisHash: sha(readFileSync(analysisPath)),
        evidence: candidate.evidence,
        status: "awaiting-review",
        recommendation: candidate.recommendation,
    };
    save(join(outputRoot, `${candidate.id}.proposal.json`), proposal);
    return proposal;
}

export function promoteRegression(proposalPath, reviewer, rationale) {
    if (!reviewer?.trim() || !rationale?.trim())
        throw new Error(
            "Promotion requires an explicit reviewer and rationale",
        );
    const owned = containedPath(repositoryRoot, proposalPath);
    if (
        !owned.startsWith(`${outputRoot}/`) &&
        !owned.startsWith(`${outputRoot}\\`)
    )
        throw new Error("Review a task-owned feedback proposal");
    const proposal = JSON.parse(readFileSync(owned, "utf8"));
    if (
        proposal.status !== "awaiting-review" ||
        ![
            "production-change-required",
            "writable-environment-required",
        ].includes(proposal.rule)
    )
        throw new Error("Unsupported regression proposal");
    if (!Array.isArray(proposal.evidence) || proposal.evidence.length === 0)
        throw new Error("Regression promotion requires source trial evidence");
    const analysisPath = join(outputRoot, "analysis.json");
    if (sha(readFileSync(analysisPath)) !== proposal.sourceAnalysisHash)
        throw new Error(
            "Source analysis changed after the proposal was drafted",
        );
    const candidate = JSON.parse(
        readFileSync(analysisPath, "utf8"),
    ).candidates.find((item) => item.id === proposal.id);
    const expectedRule =
        candidate?.theme === "no-production-change"
            ? "production-change-required"
            : candidate?.theme === "execution-environment-not-writable"
              ? "writable-environment-required"
              : null;
    if (
        expectedRule !== proposal.rule ||
        sha(JSON.stringify(candidate.evidence)) !==
            sha(JSON.stringify(proposal.evidence))
    )
        throw new Error(
            "Proposal no longer matches its grounded feedback candidate",
        );
    for (const evidence of proposal.evidence) {
        const trial = containedPath(repositoryRoot, evidence.trial);
        if (sha(readFileSync(trial)) !== evidence.trialHash)
            throw new Error("Source trial changed after review proposal");
        const record = JSON.parse(readFileSync(trial, "utf8"));
        if (
            sha(
                readFileSync(containedPath(repositoryRoot, record.eventsPath)),
            ) !== evidence.traceHash
        )
            throw new Error("Source trace changed after review proposal");
    }
    const registryPath = join(
        repositoryRoot,
        "Tooling/ai-development/agent-regressions.json",
    );
    const registry = existsSync(registryPath)
        ? JSON.parse(readFileSync(registryPath, "utf8"))
        : { schemaVersion: 1, rules: [] };
    if (registry.rules.some((rule) => rule.id === proposal.rule))
        throw new Error("Equivalent regression is already maintained");
    registry.rules.push({
        id: proposal.rule,
        reviewer,
        rationale,
        proposalHash: sha(readFileSync(owned)),
        evidenceHashes: proposal.evidence.map((evidence) => ({
            trial: evidence.trialHash,
            trace: evidence.traceHash,
        })),
        reviewedAtUtc: new Date().toISOString(),
    });
    save(registryPath, registry);
    return { rule: proposal.rule, status: "promoted", registryPath };
}

export function checkAgentRegressions(
    record,
    registry = JSON.parse(
        readFileSync(
            join(
                repositoryRoot,
                "Tooling/ai-development/agent-regressions.json",
            ),
            "utf8",
        ),
    ),
) {
    return registry.rules.map((rule) => ({
        id: rule.id,
        passed:
            rule.id === "production-change-required"
                ? (record.grade?.changedPaths?.length ?? 0) > 0
                : record.environmentWritable === true,
        status:
            rule.id === "writable-environment-required" &&
            record.environmentWritable === undefined
                ? "unverified"
                : "evaluated",
    }));
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    try {
        const [action, id, reviewer, rationale] = process.argv.slice(2);
        const result =
            action === "analyze"
                ? analyzeTrials()
                : action === "draft"
                  ? draftRegression(id)
                  : action === "promote"
                    ? promoteRegression(id, reviewer, rationale)
                    : action === "evaluate"
                      ? {
                            trial: id,
                            checks: checkAgentRegressions(
                                JSON.parse(
                                    readFileSync(
                                        containedPath(repositoryRoot, id),
                                        "utf8",
                                    ),
                                ),
                            ),
                        }
                      : null;
        if (!result)
            throw new Error(
                "Use analyze, draft <candidate> or promote <proposal-path> <reviewer> <rationale>",
            );
        if (action === "analyze")
            save(join(outputRoot, "analysis.json"), result);
        console.log(JSON.stringify(result, null, 2));
        if (
            action === "evaluate" &&
            result.checks.some((check) => !check.passed)
        )
            process.exitCode = 1;
    } catch (error) {
        console.error(redactDiagnostic(error.message));
        process.exitCode = 1;
    }
}
