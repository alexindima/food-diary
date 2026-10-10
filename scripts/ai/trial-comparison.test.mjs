import test from "node:test";
import assert from "node:assert/strict";
import { compareTrialRecords } from "./trial-comparison.mjs";

const base = {
    id: "owned",
    baseline: "same",
    cliVersion: "same",
    harnessHash: "same",
    graderEngineHash: "same",
    instructionsHash: "same",
    graderHash: "same",
    corpusHash: "same",
    dependencyLockHash: "same",
    timeoutMinutes: 20,
    status: "completed",
    passed: true,
    durationMs: 100,
    settings: { model: "default" },
};

test("a read-only environment is not a skill quality comparison", () => {
    const result = compareTrialRecords([
        {
            ...base,
            variant: "control",
            environmentWritable: false,
            passed: false,
        },
    ]);
    assert.equal(result.comparisons[0].verdict, "environment-unverified");
    assert.equal(result.comparisons[0].control.environmentFailures, 1);
});

test("different skill fingerprints never share an outcome profile", () => {
    const result = compareTrialRecords([
        { ...base, variant: "control" },
        { ...base, variant: "api-skill", skillHash: "one" },
        { ...base, variant: "api-skill", skillHash: "two", passed: false },
    ]);
    assert.equal(result.comparisons.length, 2);
    assert.equal(result.comparisons[0].skill.successes, 1);
    assert.equal(result.comparisons[1].skill.successes, 0);
});

test("running attempts do not count as failures or zero-duration completions", () => {
    const result = compareTrialRecords([
        { ...base, variant: "control" },
        {
            ...base,
            variant: "control",
            status: "running",
            passed: undefined,
            durationMs: undefined,
        },
    ]);
    assert.equal(result.comparisons[0].control.samples, 1);
    assert.equal(result.comparisons[0].control.pending, 1);
    assert.equal(result.comparisons[0].control.firstAttemptSuccessRate, 1);
    assert.equal(result.comparisons[0].control.meanDurationMs, 100);
});

test("unknown model identity stays unverified even after a nominal sample threshold", () => {
    const rows = Array.from({ length: 5 }, () => [
        { ...base, variant: "control" },
        { ...base, variant: "api-skill", skillHash: "one" },
    ]).flat();
    assert.equal(
        compareTrialRecords(rows).comparisons[0].verdict,
        "unverified-model",
    );
});

test("different observed models and reasoning budgets remain separate cohorts", () => {
    const observed = {
        ...base,
        settings: {
            requestedModel: "pinned",
            actualModel: "one",
            reasoningEffort: "medium",
        },
    };
    const result = compareTrialRecords([
        { ...observed, variant: "control" },
        { ...observed, variant: "api-skill", skillHash: "one" },
        {
            ...observed,
            variant: "api-skill",
            skillHash: "one",
            settings: { ...observed.settings, actualModel: "two" },
        },
        {
            ...observed,
            variant: "api-skill",
            skillHash: "one",
            settings: { ...observed.settings, reasoningEffort: "high" },
        },
    ]);
    assert.equal(result.comparisons.length, 3);
    assert.equal(result.comparisons[0].model, "one");
    assert.ok(result.comparisons[0].control.successInterval95[0] < 0.5);
    assert.ok(result.comparisons[0].control.successInterval95[1] > 0.99);
});
