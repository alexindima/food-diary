import test from "node:test";
import assert from "node:assert/strict";
import { mkdirSync } from "node:fs";
import { randomUUID } from "node:crypto";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";
import { acquireRuntimeCheck } from "./runtime-check-lease.mjs";

test("concurrent browser, load or recovery work cannot invalidate the source snapshot", () => {
    const state = {
        id: randomUUID(),
        directory: join(repositoryRoot, ".artifacts/lease-test", randomUUID()),
    };
    mkdirSync(state.directory, { recursive: true });
    const release = acquireRuntimeCheck(state, "recovery");
    try {
        assert.throws(
            () => acquireRuntimeCheck(state, "browser"),
            /sequential/u,
        );
    } finally {
        release();
    }
    acquireRuntimeCheck(state, "load")();
});
