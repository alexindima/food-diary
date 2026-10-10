import { randomUUID } from "node:crypto";
import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";
import { startRuntime, stopRuntime } from "./task-runtime.mjs";

const suffix = randomUUID().slice(0, 8);
const names = [`probe-a-${suffix}`, `probe-b-${suffix}`];
const states = [];
try {
    for (const name of names) states.push(await startRuntime(name));
    const auth = [];
    for (const state of states) {
        const credentials = JSON.parse(
            readFileSync(state.credentialsPath, "utf8"),
        );
        const response = await fetch(`${state.apiUrl}/api/v1/auth/login`, {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify(credentials),
        });
        if (!response.ok)
            throw new Error(`Owned login failed (${response.status})`);
        auth.push(await response.json());
    }
    const entries = async (index) => {
        const response = await fetch(
            `${states[index].apiUrl}/api/v1/hydrations`,
            { headers: { authorization: `Bearer ${auth[index].accessToken}` } },
        );
        if (!response.ok)
            throw new Error(`Owned read failed (${response.status})`);
        return response.json();
    };
    const before = await Promise.all([entries(0), entries(1)]);
    const write = await fetch(`${states[0].apiUrl}/api/v1/hydrations`, {
        method: "POST",
        headers: {
            authorization: `Bearer ${auth[0].accessToken}`,
            "content-type": "application/json",
            "Idempotency-Key": randomUUID(),
        },
        body: JSON.stringify({
            timestampUtc: new Date().toISOString(),
            amountMl: 250,
        }),
    });
    if (write.status !== 201)
        throw new Error(`Owned write failed (${write.status})`);
    const after = await Promise.all([entries(0), entries(1)]);
    const result = {
        bothReady: states.every((state) => state.status === "ready"),
        uniquePorts:
            new Set(states.flatMap((state) => Object.values(state.ports)))
                .size === 12,
        differentAccounts: auth[0].user.id !== auth[1].user.id,
        writeVisibleOnlyToOwner:
            after[0].length === before[0].length + 1 &&
            after[1].length === before[1].length,
        browserProbes: states.map((state) => state.browserProbe),
        runtimeNames: names,
    };
    if (
        !result.bothReady ||
        !result.uniquePorts ||
        !result.differentAccounts ||
        !result.writeVisibleOnlyToOwner
    )
        throw new Error("Owned runtime isolation failed");
    writeFileSync(
        join(
            repositoryRoot,
            ".artifacts/ai-development-foundation/runtime-isolation-proof.json",
        ),
        JSON.stringify(result, null, 2),
    );
    console.log(JSON.stringify(result));
} catch (error) {
    console.error(error.message);
    process.exitCode = 1;
} finally {
    for (const state of [...states].reverse()) {
        try {
            stopRuntime(state.name);
        } catch (error) {
            console.error(error.message);
            process.exitCode = 1;
        }
    }
}
