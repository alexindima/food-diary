import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import {
    mkdirSync,
    mkdtempSync,
    readFileSync,
    rmSync,
    writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { createServer } from "node:http";
import { join } from "node:path";
import test from "node:test";
import { repositoryRoot } from "./generate-feature.mjs";
import {
    freePorts,
    listRuntimes,
    processIdentity,
    runtimeDirectory,
    safeSettings,
    stopOwnedProcess,
    stopRuntime,
} from "./task-runtime.mjs";

test("runtime discovery returns safe metadata and never claims dead or unprobed owners are ready", async () => {
    const root = mkdtempSync(join(tmpdir(), "fd-runtime-discovery-"));
    try {
        const directory = join(root, ".artifacts", "task-runtime", "sample");
        mkdirSync(directory, { recursive: true });
        const state = {
            checkout: root,
            id: randomUUID(),
            status: "ready",
            frontendUrl: "http://127.0.0.1:4200/path",
            apiUrl: "http://127.0.0.1:5000",
            processes: [{ pid: process.pid, identity: "different-lifetime" }],
            password: "private-sentinel",
            token: "private-sentinel",
            credentialsPath: "private-sentinel",
        };
        writeFileSync(join(directory, "runtime.json"), JSON.stringify(state));
        const result = await listRuntimes(root, {
            includeWorktrees: false,
            probe: false,
        });
        assert.equal(result.runtimes.length, 1);
        assert.equal(result.runtimes[0].frontendUrl, "http://127.0.0.1:4200");
        assert.equal(result.runtimes[0].observedStatus, "unverified");
        assert.equal(result.runtimes[0].ownedProcessesAlive, false);
        assert.ok(!JSON.stringify(result).includes("private-sentinel"));
        state.frontendUrl = "https://example.invalid/";
        state.apiUrl = "http://secret:private-sentinel@localhost:5000/";
        writeFileSync(join(directory, "runtime.json"), JSON.stringify(state));
        const blocked = await listRuntimes(root, { includeWorktrees: false });
        assert.equal(blocked.runtimes[0].frontendUrl, null);
        assert.equal(blocked.runtimes[0].apiUrl, null);
        assert.ok(!JSON.stringify(blocked).includes("private-sentinel"));
        writeFileSync(join(directory, "runtime.json"), "{");
        const malformed = await listRuntimes(root, {
            includeWorktrees: false,
            includeInvalid: true,
        });
        assert.equal(malformed.runtimes[0].observedStatus, "invalid-record");
        assert.equal(malformed.invalidRecordCount, 1);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("runtime discovery probes the matching owned process before reporting ready", async () => {
    const root = mkdtempSync(join(tmpdir(), "fd-runtime-live-"));
    const server = createServer((request, response) => {
        response.writeHead(200);
        response.end(request.url === "/health/ready" ? "Healthy" : "frontend");
    });
    await new Promise((resolveListen) =>
        server.listen(0, "127.0.0.1", resolveListen),
    );
    try {
        const directory = join(root, ".artifacts", "task-runtime", "live");
        mkdirSync(directory, { recursive: true });
        const url = `http://127.0.0.1:${server.address().port}`;
        writeFileSync(
            join(directory, "runtime.json"),
            JSON.stringify({
                id: randomUUID(),
                checkout: root,
                status: "ready",
                frontendUrl: url,
                apiUrl: url,
                processes: [
                    {
                        pid: process.pid,
                        identity: processIdentity(process.pid),
                    },
                ],
            }),
        );
        const result = await listRuntimes(root, { includeWorktrees: false });
        assert.equal(result.runtimes[0].observedStatus, "ready");
        assert.equal(result.runtimes[0].apiReady, true);
        assert.equal(result.runtimes[0].frontendResponding, true);
    } finally {
        await new Promise((resolveClose) => server.close(resolveClose));
        rmSync(root, { recursive: true, force: true });
    }
});

test("owned settings clear credentials while retaining non-secret password-reset paths", () => {
    const name = `settings-${randomUUID().slice(0, 8)}`;
    const directory = runtimeDirectory(name);
    mkdirSync(directory, { recursive: true });
    safeSettings(
        repositoryRoot,
        directory,
        {
            api: 51001,
            frontend: 51002,
            postgres: 51003,
            redis: 51004,
            storage: 51005,
            providers: 51006,
        },
        randomUUID(),
    );
    const configuration = JSON.parse(
        readFileSync(join(directory, "appsettings.json"), "utf8"),
    );
    assert.equal(configuration.OpenAi.ApiKey, "");
    assert.ok(configuration.Email.PasswordResetPath.length > 0);
    assert.equal(configuration.MailInboxClient.AllowInsecureLoopback, true);
    assert.ok(
        configuration.ConnectionStrings.DefaultConnection.startsWith(
            "Host=127.0.0.1;",
        ),
    );
    assert.deepEqual(configuration.Cors.Origins, ["http://127.0.0.1:51002"]);
});

test("runtime names cannot escape their artifact directory", () => {
    assert.throws(() => runtimeDirectory("../shared"), /slug/u);
    assert.throws(() => runtimeDirectory("a/../../outside"), /slug/u);
});

test("port allocation keeps each requested socket distinct", async () => {
    const ports = await freePorts(6);
    assert.equal(new Set(ports).size, 6);
    assert.ok(ports.every((port) => Number.isInteger(port) && port > 0));
});

test("PID identity mismatch never stops a reused process", () => {
    assert.ok(processIdentity(process.pid));
    assert.throws(
        () =>
            stopOwnedProcess({
                pid: process.pid,
                identity: "another-lifetime",
            }),
        /reused PID/u,
    );
});

test("cleanup refuses a runtime record belonging to another checkout", () => {
    const name = `guard-${randomUUID().slice(0, 8)}`;
    const directory = runtimeDirectory(name);
    mkdirSync(directory, { recursive: true });
    writeFileSync(
        join(directory, "runtime.json"),
        JSON.stringify({
            checkout: `${repositoryRoot}-foreign`,
            directory,
            processes: [],
            containers: [],
        }),
    );
    assert.throws(() => stopRuntime(name), /another checkout/u);
});
