import test from "node:test";
import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { mkdirSync, readFileSync, unlinkSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";
import {
    cleanStoppedBuild,
    diskBudget,
    ownedBuildScope,
    recordBuildScope,
} from "./resource-budget.mjs";

test("low disk is reported before expensive runtime allocation", () => {
    assert.equal(
        diskBudget(repositoryRoot, 3, () => ({ bavail: 2, bsize: 1024 ** 3 }))
            .ready,
        false,
    );
    assert.equal(
        diskBudget(repositoryRoot, 3, () => ({ bavail: 3, bsize: 1024 ** 3 }))
            .ready,
        true,
    );
    assert.throws(
        () => diskBudget(repositoryRoot, 3, () => ({ bavail: NaN, bsize: 1 })),
        /measured/u,
    );
});

test("only recorded stopped scopes use native clean and retain evidence", () => {
    const id = randomUUID();
    const name = `resource-${id.slice(0, 8)}`;
    const state = {
        id,
        name,
        checkout: repositoryRoot,
        directory: join(repositoryRoot, ".artifacts/task-runtime", name),
        status: "stopped",
        failures: [],
        processes: [],
    };
    const scope = recordBuildScope(state);
    const report = join(state.directory, "evidence.json");
    mkdirSync(state.directory, { recursive: true });
    writeFileSync(report, '{"passed":true}');
    const calls = [];
    try {
        cleanStoppedBuild(state, repositoryRoot, (command, args) => {
            calls.push({ command, args });
            return { status: 0 };
        });
        assert.equal(calls.length, 2);
        assert.ok(
            calls.every(
                (call) =>
                    call.command === "dotnet" &&
                    call.args[0] === "clean" &&
                    call.args.includes(scope.build),
            ),
        );
        assert.equal(readFileSync(report, "utf8"), '{"passed":true}');
        assert.throws(
            () => cleanStoppedBuild({ ...state, status: "ready" }),
            /stopped/u,
        );
        assert.throws(
            () =>
                cleanStoppedBuild({
                    ...state,
                    processes: [{ pid: process.pid }],
                }),
            /alive/u,
        );
    } finally {
        unlinkSync(report);
        unlinkSync(
            join(repositoryRoot, ".artifacts/resource-scopes", `${id}.json`),
        );
    }
});

test("foreign paths and forged receipt identities cannot trigger cleanup", () => {
    const state = {
        name: "resource-proof",
        id: randomUUID(),
        checkout: repositoryRoot,
        directory: join(
            repositoryRoot,
            ".artifacts/task-runtime/resource-proof",
        ),
    };
    assert.throws(
        () => ownedBuildScope({ ...state, directory: repositoryRoot }),
        /Foreign/u,
    );
    assert.throws(
        () => ownedBuildScope({ ...state, id: "../../outside" }),
        /identity/u,
    );
    assert.throws(
        () => ownedBuildScope({ ...state, name: "../outside" }),
        /identity/u,
    );
    assert.throws(
        () =>
            cleanStoppedBuild({
                ...state,
                status: "stopped",
                processes: [],
                failures: [],
            }),
        /receipt/u,
    );
});
