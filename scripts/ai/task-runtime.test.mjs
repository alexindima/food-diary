import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";
import { repositoryRoot } from "./generate-feature.mjs";
import {
    freePorts,
    processIdentity,
    runtimeDirectory,
    safeSettings,
    stopOwnedProcess,
    stopRuntime,
} from "./task-runtime.mjs";

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
