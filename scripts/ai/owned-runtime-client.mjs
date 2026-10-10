import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";
import { runtimeDirectory } from "./task-runtime.mjs";
import { runtimeDiagnosticState } from "./runtime-diagnostics.mjs";

export function ownedRuntime(name, root = repositoryRoot) {
    const diagnostic = runtimeDiagnosticState(name, root);
    if (
        diagnostic.status !== "ready" ||
        diagnostic.staleSources ||
        diagnostic.processes.some((process) => !process.alive)
    )
        throw new Error("A current, ready owned runtime is required");
    return JSON.parse(
        readFileSync(
            join(runtimeDirectory(name, root), "runtime.json"),
            "utf8",
        ),
    );
}

export function ownedContainer(state, role) {
    const owner = state.containers.find((container) => container.role === role);
    if (!owner || !/^[a-f0-9]{12,64}$/u.test(owner.id))
        throw new Error("Owned container identity is missing");
    const result = spawnSync(
        "docker",
        [
            "inspect",
            "--format",
            '{{ index .Config.Labels "fooddiary.task" }}',
            owner.id,
        ],
        { encoding: "utf8", windowsHide: true },
    );
    if (result.status !== 0 || result.stdout.trim() !== state.id)
        throw new Error("Container ownership could not be proved");
    return owner.id;
}

export async function ownedApi(name, root = repositoryRoot) {
    const state = ownedRuntime(name, root);
    const credentials = JSON.parse(
        readFileSync(join(state.directory, "credentials.json"), "utf8"),
    );
    const loginRequest = () =>
        fetch(`${state.apiUrl}/api/v1/auth/login`, {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify(credentials),
            redirect: "error",
            signal: AbortSignal.timeout(30_000),
        });
    let login = await loginRequest();
    for (let attempt = 0; login.status === 429 && attempt < 2; attempt++) {
        const seconds = Math.min(
            60,
            Math.max(1, Number(login.headers.get("retry-after")) || 60),
        );
        console.log("Owned login is rate limited; respecting its retry window");
        await new Promise((resolveWait) =>
            setTimeout(resolveWait, seconds * 1000),
        );
        login = await loginRequest();
    }
    if (login.status !== 200)
        throw new Error(`Owned login failed (${login.status})`);
    const auth = await login.json();
    if (typeof auth.accessToken !== "string")
        throw new Error("Owned login returned no access token");
    const token = auth.accessToken;
    return {
        state,
        // This value is used only in an ignored env file or in memory; never log the client.
        token,
        async request(method, path, body, expected = [200]) {
            if (!path.startsWith("/api/v1/") || path.includes("://"))
                throw new Error("Only owned relative API paths are permitted");
            const response = await fetch(`${state.apiUrl}${path}`, {
                method,
                headers: {
                    authorization: `Bearer ${token}`,
                    "content-type": "application/json",
                },
                ...(body === undefined ? {} : { body: JSON.stringify(body) }),
                redirect: "error",
                signal: AbortSignal.timeout(60_000),
            });
            if (!expected.includes(response.status))
                throw new Error(
                    `Owned ${method} failed with HTTP ${response.status}`,
                );
            return response.status === 204 ? null : await response.json();
        },
    };
}
