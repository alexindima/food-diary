import test from "node:test";
import assert from "node:assert/strict";
import {
    safeJourneyOutput,
    sanitizeJourneyReport,
} from "./journey-evidence.mjs";

test("locator call logs redact fixture passwords even without a password key", () => {
    const password = "Synthetic!Secret42";
    const line = `filling \"${password}\"`;
    assert.equal(safeJourneyOutput(line, [password]).includes(password), false);
    const report = sanitizeJourneyReport({ suites: [{ errors: [line] }] }, [
        password,
    ]);
    assert.equal(JSON.stringify(report).includes(password), false);
});
