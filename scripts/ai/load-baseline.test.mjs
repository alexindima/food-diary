import test from "node:test";
import assert from "node:assert/strict";
import { loadPlan } from "./load-baseline.mjs";

test("load work is bounded and contains repeated distinct concurrency levels", () => {
    assert.deepEqual(loadPlan().levels, [1, 5, 10]);
    assert.equal(loadPlan().repetitions, 2);
    for (const options of [
        { products: 0 },
        { levels: [10000] },
        { repetitions: 0 },
        { holdSeconds: 3600 },
    ])
        assert.throws(() => loadPlan(options));
});
