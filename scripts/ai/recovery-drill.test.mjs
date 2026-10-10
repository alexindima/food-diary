import test from "node:test";
import assert from "node:assert/strict";
import { restoreDatabaseName, sameSnapshot } from "./recovery-drill.mjs";

test("restore never targets a source, production or caller-supplied SQL identifier", () => {
    assert.equal(
        restoreDatabaseName("fd_restore_0123abcd"),
        "fd_restore_0123abcd",
    );
    for (const value of [
        "fooddiary_task",
        "postgres",
        "production",
        "fd_restore_0123abcd;DROP",
        "../other",
    ])
        assert.throws(() => restoreDatabaseName(value));
});

test("equal row counts do not hide changed restored contents or missing tables", () => {
    const source = [
        { table: "A", rows: 1, digest: "abc" },
        { table: "B", rows: 0, digest: "empty" },
    ];
    assert.equal(sameSnapshot(source, source.toReversed()), true);
    assert.equal(
        sameSnapshot(source, [
            { table: "A", rows: 1, digest: "changed" },
            source[1],
        ]),
        false,
    );
    assert.equal(sameSnapshot(source, [source[0]]), false);
});
