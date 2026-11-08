import test from "node:test";
import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { spawn } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, unlinkSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";
import {
    cleanStoppedBuild,
    diskBudget,
    ownedBuildScope,
    recordBuildScope,
    waitForStoppedProcesses,
} from "./resource-budget.mjs";

test("cleanup waits for actual process exit and refuses live or invalid records", async () => {
    const state = { status: "stopped", processes: [{ pid: process.pid }] };
    await assert.rejects(waitForStoppedProcesses(state, { timeoutMilliseconds: 20 }), /did not exit/u);
    await assert.rejects(waitForStoppedProcesses({ ...state, status: "ready" }), /fully stopped/u);
    await assert.rejects(waitForStoppedProcesses({ ...state, processes: [{ pid: 0 }] }), /identity/u);
    await assert.rejects(waitForStoppedProcesses(state, { timeoutMilliseconds: 0 }), /timeout/u);
});

test("Linux children are reaped before owned native build cleanup", { skip: process.platform !== "linux" }, async () => {
    const child = spawn(process.execPath, ["-e", "process.exit(0)"], { stdio: "ignore", windowsHide: true });
    child.unref();
    const id = randomUUID();
    const name = `resource-${id.slice(0, 8)}`;
    const state = {
        id, name, checkout: repositoryRoot,
        directory: join(repositoryRoot, ".artifacts/task-runtime", name),
        status: "stopped", failures: [], processes: [{ pid: child.pid }],
    };
    const receipt = join(repositoryRoot, ".artifacts/resource-scopes", `${id}.json`);
    try {
        const deadline = performance.now() + 5000;
        let zombie = false;
        // Deliberately hold this parent event loop to reproduce the wrapper's
        // synchronous stop/clean sequence, not a mocked liveness classifier.
        while (performance.now() < deadline) {
            const stat = readFileSync(`/proc/${child.pid}/stat`, "utf8");
            zombie = stat.slice(stat.lastIndexOf(") ") + 2).startsWith("Z ");
            if (zombie) break;
            Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 10);
        }
        assert.equal(zombie, true, "Fixture must expose an exited child awaiting parent reaping");
        recordBuildScope(state);
        assert.throws(() => cleanStoppedBuild(state), /still alive/u);
        await waitForStoppedProcesses(state);
        const calls = [];
        cleanStoppedBuild(state, repositoryRoot, (command, args) => { calls.push({ command, args }); return { status: 0 }; });
        assert.equal(calls.length, 2);
        assert.ok(calls.every(call => call.command === "dotnet" && call.args[0] === "clean"));
    } finally {
        if (existsSync(receipt)) unlinkSync(receipt);
        child.kill();
    }
});

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
