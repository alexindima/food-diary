import test from "node:test";
import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { mkdirSync, writeFileSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { isolatedRuntimeProbes } from "./coding-probe-isolation.mjs";
import { repositoryRoot } from "./generate-feature.mjs";

test("generated code can use Node APIs only inside the scoped read-only network-free container", () => {
    const root = join(
        repositoryRoot,
        `.artifacts/coding-probe-tests/${randomUUID()}`,
    );
    const models = join(
        root,
        "candidate/FoodDiary.Web.Client/src/app/shared/models/semantics",
    );
    mkdirSync(models, { recursive: true });
    const marker = join(root, "private-host-marker");
    writeFileSync(marker, "private host fixture");
    const protectedFile = join(models, "protected.txt");
    writeFileSync(protectedFile, "unchanged");
    writeFileSync(
        join(models, "isolation-proof.ts"),
        `
const fs = require("node:fs");
export function candidateReadDenied() {
    try { fs.readFileSync("/private-host-marker"); return false; }
    catch (error) { return error.code === "ENOENT"; }
}
export function candidateWriteDenied() {
    try { fs.writeFileSync("/candidate/FoodDiary.Web.Client/src/app/shared/models/semantics/protected.txt", "changed"); return false; }
    catch (error) { return error.code === "EROFS" || error.code === "EACCES"; }
}
export function networkDenied() {
    return fs.readFileSync("/proc/net/route", "utf8").trim().split("\\n").length === 1;
}
`,
    );
    const result = isolatedRuntimeProbes(
        "isolation-proof",
        join(root, "candidate"),
    );
    assert.equal(result.passed, true);
    assert.equal(readFileSync(protectedFile, "utf8"), "unchanged");
    assert.equal(readFileSync(marker, "utf8"), "private host fixture");
});

test("the isolated reference retains all existing native-value and conversion expectations", () => {
    for (const id of [
        "meal-identity",
        "calendar-meaning",
        "recipe-quantity",
        "usda-link",
        "cycle-episode",
    ]) {
        assert.equal(isolatedRuntimeProbes(id, repositoryRoot).passed, true);
    }
});
