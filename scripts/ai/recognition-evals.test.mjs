import test from "node:test";
import assert from "node:assert/strict";
import { referenceCorpus } from "./recognition-corpus.mjs";
import { gradeRecognition, referenceReplies } from "./recognition-evals.mjs";

test("the versioned corpus distinguishes text/OCR evidence from food-photo quality", () => {
    const corpus = referenceCorpus();
    const result = gradeRecognition(corpus, referenceReplies(corpus));
    assert.equal(result.passed, 20);
    assert.equal(result.coverage.realFoodPhoto, 0);
    assert.equal(result.providerCallsMadeByGrader, 0);
    assert.equal(result.p95LatencyMs, null);
    assert.equal(result.totalReportedCostUsd, null);
});

test("wrong quantity, foreign unit and invented unreadable nutrition fail independently", () => {
    const corpus = referenceCorpus();
    const candidate = referenceReplies(corpus);
    candidate.replies.find(
        (reply) => reply.caseId === "apple-100",
    ).response.items[0].amount = 1;
    candidate.replies.find(
        (reply) => reply.caseId === "milk-ml",
    ).response.items[0].unit = "g";
    candidate.replies.find(
        (reply) => reply.caseId === "label-unreadable",
    ).response.calories = 0;
    const result = gradeRecognition(corpus, candidate);
    assert.equal(result.passed, 17);
    assert.ok(
        result.cases
            .find((item) => item.id === "label-unreadable")
            .errors.includes("label-calories"),
    );
});

test("missing cases lower accuracy; duplicated cases and mismatched corpora cannot hide failures", () => {
    const corpus = referenceCorpus();
    const candidate = referenceReplies(corpus);
    candidate.replies.pop();
    assert.equal(gradeRecognition(corpus, candidate).passed, 19);
    candidate.replies.push(candidate.replies[0]);
    assert.throws(() => gradeRecognition(corpus, candidate), /Duplicate/u);
    assert.throws(
        () =>
            gradeRecognition(corpus, {
                ...referenceReplies(corpus),
                corpusSha256: "wrong",
            }),
        /corpus/u,
    );
});
