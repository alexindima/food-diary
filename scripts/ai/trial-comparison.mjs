// Compare configuration cohorts, retaining incomplete and incompatible trials explicitly.
const terminal = new Set([
    "completed",
    "failed",
    "timed-out",
    "cancelled",
    "interrupted",
]);

function summary(rows, variant, skillHash = null) {
    const selected = rows.filter(
        (row) =>
            row.variant === variant &&
            (variant !== "api-skill" || (row.skillHash ?? null) === skillHash),
    );
    const finished = selected.filter((row) => terminal.has(row.status));
    const durations = finished
        .map((row) => row.durationMs)
        .filter((value) => Number.isFinite(value) && value >= 0);
    const measured = finished.filter((row) => row.usage);
    const total = (field) =>
        measured.reduce((sum, row) => sum + (Number(row.usage[field]) || 0), 0);
    const successes = finished.filter((row) => row.passed === true).length;
    const count = finished.length;
    // Wilson interval stays uncertain for small samples, including 0% and 100%.
    const z = 1.96,
        p = count ? successes / count : 0;
    const denominator = 1 + (z * z) / (count || 1);
    const center = (p + (z * z) / (2 * (count || 1))) / denominator;
    const margin =
        (z *
            Math.sqrt(
                (p * (1 - p)) / (count || 1) +
                    (z * z) / (4 * (count || 1) ** 2),
            )) /
        denominator;
    const sortedDurations = [...durations].sort((left, right) => left - right);
    return {
        variant,
        skillHash,
        samples: finished.length,
        pending: selected.length - finished.length,
        completed: finished.filter((row) => row.status === "completed").length,
        successes,
        commandTelemetrySamples: finished.filter(
            (row) => typeof row.failedToolCalls === "number",
        ).length,
        observedPolicyRejections: finished.filter(
            (row) => row.executionRejection,
        ).length,
        environmentFailures: finished.filter(
            (row) =>
                row.environmentWritable === false ||
                [
                    "execution-policy-rejected",
                    "execution-start-rejected",
                ].includes(row.failureCategory),
        ).length,
        successInterval95: count
            ? [Math.max(0, center - margin), Math.min(1, center + margin)]
            : null,
        medianDurationMs: sortedDurations.length
            ? (sortedDurations[Math.floor((sortedDurations.length - 1) / 2)] +
                  sortedDurations[Math.floor(sortedDurations.length / 2)]) /
              2
            : null,
        failedToolCalls: finished.reduce(
            (sum, row) => sum + (row.failedToolCalls ?? 0),
            0,
        ),
        interventions: finished.reduce(
            (sum, row) => sum + (row.interventions ?? 0),
            0,
        ),
        firstAttemptSuccessRate: finished.length
            ? finished.filter((row) => row.passed === true).length /
              finished.length
            : null,
        meanDurationMs: durations.length
            ? durations.reduce((sum, value) => sum + value, 0) /
              durations.length
            : null,
        durationSamples: durations.length,
        usageAvailable: measured.length,
        usage: measured.length
            ? {
                  inputTokens: total("input_tokens"),
                  cachedInputTokens: total("cached_input_tokens"),
                  outputTokens: total("output_tokens"),
              }
            : null,
    };
}

export function compareTrialRecords(records) {
    const cohorts = new Map();
    for (const row of records) {
        const key = JSON.stringify([
            row.id,
            row.baseline,
            row.cliVersion,
            row.harnessHash ?? row.recordPath,
            row.graderEngineHash ?? row.recordPath,
            row.instructionsHash,
            row.graderHash,
            row.corpusHash,
            row.dependencyLockHash,
            row.timeoutMinutes,
            row.settings ?? null,
        ]);
        const rows = cohorts.get(key) ?? [];
        rows.push(row);
        cohorts.set(key, rows);
    }
    const comparisons = [...cohorts.values()].flatMap((rows) => {
        const revisions = [
            ...new Set(
                rows
                    .filter((row) => row.variant === "api-skill")
                    .map((row) => row.skillHash ?? null),
            ),
        ];
        return (revisions.length ? revisions : [null]).map((revision) => {
            const control = summary(rows, "control"),
                skill = summary(rows, "api-skill", revision);
            const modelIdentityVerified = rows.every(
                (row) =>
                    typeof row.settings?.actualModel === "string" &&
                    row.settings.actualModel.length > 0,
            );
            return {
                id: rows[0].id,
                model: rows[0].settings?.actualModel ?? null,
                requestedModel: rows[0].settings?.requestedModel ?? null,
                reasoningEffort: rows[0].settings?.reasoningEffort ?? null,
                control,
                skill,
                modelIdentityVerified,
                verdict:
                    control.environmentFailures > 0 ||
                    skill.environmentFailures > 0
                        ? "environment-unverified"
                        : control.samples < 5 || skill.samples < 5
                          ? "insufficient-data"
                          : !modelIdentityVerified
                            ? "unverified-model"
                            : "requires-statistical-review",
            };
        });
    });
    return {
        schemaVersion: 2,
        trials: records.length,
        terminalTrials: records.filter((row) => terminal.has(row.status))
            .length,
        pendingTrials: records.filter((row) => !terminal.has(row.status))
            .length,
        comparisons,
        recommendation:
            "Do not claim productivity gains from grader self-tests, small unmatched samples or an unreported model identity.",
    };
}
