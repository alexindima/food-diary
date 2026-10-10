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
    return {
        variant,
        skillHash,
        samples: finished.length,
        pending: selected.length - finished.length,
        completed: finished.filter((row) => row.status === "completed").length,
        successes: finished.filter((row) => row.passed === true).length,
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
            const modelIdentityVerified =
                typeof rows[0].settings?.actualModel === "string";
            return {
                id: rows[0].id,
                control,
                skill,
                modelIdentityVerified,
                verdict:
                    control.samples < 5 || skill.samples < 5
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
