import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { execFileSync } from "node:child_process";
import {
    mkdirSync,
    mkdtempSync,
    existsSync,
    readFileSync,
    rmSync,
    writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { createServer } from "node:http";
import { isAbsolute, join, resolve } from "node:path";
import test from "node:test";
import { repositoryRoot } from "./generate-feature.mjs";
import { prepareTaskFrontendWorkspace } from "./task-frontend-workspace.mjs";
import { watchFrontendIdentity, writeFrontendIdentity } from "./task-frontend-identity.mjs";
import {
    freePorts,
    listRuntimes,
    processIdentity,
    runtimeDirectory,
    safeSettings,
    sourceFingerprint,
    stopOwnedProcess,
    stopRuntime,
    taskFrontendEnvironment,
} from "./task-runtime.mjs";

test("task frontend SSR always targets its owned API instead of an inherited runtime", () => {
    const inherited = { SSR_API_ORIGIN: "http://localhost:5300", PATH: "existing-path" };
    assert.deepEqual(taskFrontendEnvironment("http://127.0.0.1:54512", inherited), {
        SSR_API_ORIGIN: "http://127.0.0.1:54512",
        PATH: "existing-path",
    });
    assert.equal(inherited.SSR_API_ORIGIN, "http://localhost:5300");
});

test("runtime freshness includes extracted WebMCP library source", () => {
    const root = mkdtempSync(join(tmpdir(), "fd-webmcp-fingerprint-"));
    try {
        const git = (...args) => execFileSync("git", ["-C", root, ...args], { encoding: "utf8", windowsHide: true });
        git("init", "-q");
        git("-c", "user.name=Fixture", "-c", "user.email=fixture@example.test", "commit", "--allow-empty", "-qm", "fixture");
        const directory = join(root, "FoodDiary.Web.Client", "projects", "fd-webmcp", "src", "lib");
        mkdirSync(directory, { recursive: true });
        const baseline = sourceFingerprint(root);
        writeFileSync(join(directory, "bridge.ts"), "export const revision = 1;\n");
        const created = sourceFingerprint(root);
        assert.notEqual(created, baseline);
        writeFileSync(join(directory, "bridge.ts"), "export const revision = 2;\n");
        const changed = sourceFingerprint(root);
        assert.notEqual(changed, created);
        writeFileSync(join(directory, "bridge.spec.ts"), "// test-only change\n");
        assert.equal(sourceFingerprint(root), changed);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

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

test("admin workspace keeps shared sources unchanged and uses separate local auth origins", () => {
    const root = mkdtempSync(join(tmpdir(), "fd-admin-workspace-"));
    try {
        const client = join(root, "FoodDiary.Web.Client");
        mkdirSync(join(client, "node_modules"), { recursive: true });
        const source = readFileSync(
            join(repositoryRoot, "FoodDiary.Web.Client/angular.json"),
            "utf8",
        );
        writeFileSync(join(client, "angular.json"), source);
        const directory = join(root, ".artifacts/task-runtime/sample");
        const workspace = prepareTaskFrontendWorkspace(root, directory, {
            frontendUrl: "http://127.0.0.1:51002",
            adminUrl: "http://127.0.0.1:51007",
        });
        const generated = JSON.parse(
            readFileSync(join(workspace, "angular.json"), "utf8"),
        );
        assert.equal(
            readFileSync(join(client, "angular.json"), "utf8"),
            source,
        );
        assert.match(
            readFileSync(join(workspace, "environment.client.ts"), "utf8"),
            /http:\/\/127\.0\.0\.1:51007/u,
        );
        const adminEnvironment = readFileSync(
            join(workspace, "environment.admin.ts"),
            "utf8",
        );
        assert.match(adminEnvironment, /http:\/\/127\.0\.0\.1:51002/u);
        assert.match(adminEnvironment, /auth: '\/api\/v1\/auth'/u);
        assert.match(adminEnvironment, /buildVersion: 'task'/u);
        for (const project of Object.values(generated.projects)) {
            const build = project.architect.build;
            for (const asset of build.options.assets)
                assert.ok(
                    resolve(workspace, asset.input).startsWith(workspace),
                );
            for (const replacement of build.configurations.task
                .fileReplacements) {
                assert.equal(isAbsolute(replacement.with), false);
                assert.ok(existsSync(resolve(workspace, replacement.with)));
            }
            assert.ok(
                resolve(workspace, build.options.outputPath.base).startsWith(
                    directory,
                ),
            );
        }
        assert.equal(
            prepareTaskFrontendWorkspace(root, directory, {
                frontendUrl: "http://127.0.0.1:51002",
                adminUrl: "http://127.0.0.1:51007",
            }),
            workspace,
        );
    } finally {
        assert.ok(
            resolve(root).startsWith(resolve(tmpdir()) + "/") ||
                resolve(root).startsWith(resolve(tmpdir()) + "\\"),
        );
        assert.ok(root.includes("fd-admin-workspace-"));
        rmSync(root, { recursive: true, force: true });
    }
});

test("admin workspace refuses external URLs, credential URLs, same-origin storage and foreign paths", () => {
    const directory = join(
        repositoryRoot,
        ".artifacts/task-runtime/guard-test",
    );
    for (const adminUrl of [
        "https://example.test",
        "http://secret:sentinel@127.0.0.1:51007",
        "http://127.0.0.1:51007/path",
        "http://127.0.0.1:51007/?token=sentinel",
        "http://127.0.0.1:51002",
    ]) {
        assert.throws(() =>
            prepareTaskFrontendWorkspace(repositoryRoot, directory, {
                frontendUrl: "http://127.0.0.1:51002",
                adminUrl,
            }),
        );
    }
    assert.throws(
        () =>
            prepareTaskFrontendWorkspace(
                repositoryRoot,
                join(repositoryRoot, "FoodDiary.Web.Client"),
                {
                    frontendUrl: "http://127.0.0.1:51002",
                    adminUrl: "http://127.0.0.1:51007",
                },
            ),
        /one task runtime/u,
    );
});

test("frontend source identity updates with live sources and task configuration without exposing contents", async () => {
    const root = mkdtempSync(join(tmpdir(), "fd-frontend-identity-"));
    let stop;
    try {
        const client = join(root, "FoodDiary.Web.Client");
        mkdirSync(join(client, "node_modules"), { recursive: true });
        mkdirSync(join(client, "src"));
        writeFileSync(join(client, "angular.json"), readFileSync(join(repositoryRoot, "FoodDiary.Web.Client/angular.json")));
        const source = join(client, "src/probe.ts");
        writeFileSync(source, "export const value = 'private-sentinel';");
        const directory = join(root, ".artifacts/task-runtime/identity");
        const workspace = prepareTaskFrontendWorkspace(root, directory, { frontendUrl: "http://127.0.0.1:51002" });
        const original = writeFrontendIdentity(root, workspace);
        assert.equal(original.revision, null);
        assert.equal(JSON.stringify(original).includes("private-sentinel"), false);
        assert.equal(writeFrontendIdentity(root, workspace).capturedAtUtc, original.capturedAtUtc);
        writeFileSync(join(client, "src/probe.spec.ts"), "test only");
        assert.equal(writeFrontendIdentity(root, workspace).sourceFingerprint, original.sourceFingerprint);
        const changed = await new Promise((resolveChanged, rejectChanged) => {
            const timeout = setTimeout(() => rejectChanged(new Error("No source identity update was observed")), 10000);
            stop = watchFrontendIdentity(root, workspace, metadata => {
                if (metadata.sourceFingerprint !== original.sourceFingerprint) {
                    clearTimeout(timeout);
                    resolveChanged(metadata);
                }
            });
            writeFileSync(source, "export const value = 'updated-private-sentinel';");
        });
        assert.notEqual(changed.sourceFingerprint, original.sourceFingerprint);
        prepareTaskFrontendWorkspace(root, directory, { frontendUrl: "http://127.0.0.1:51002", adminUrl: "http://127.0.0.1:51007" });
        assert.notEqual(writeFrontendIdentity(root, workspace).sourceFingerprint, changed.sourceFingerprint);
        assert.equal(readFileSync(join(workspace, "frontend-build.ts"), "utf8").includes("private-sentinel"), false);
    } finally {
        stop?.();
        assert.ok(resolve(root).startsWith(resolve(tmpdir()) + "/") || resolve(root).startsWith(resolve(tmpdir()) + "\\"));
        assert.ok(root.includes("fd-frontend-identity-"));
        rmSync(root, { recursive: true, force: true });
    }
});
