import {
    existsSync,
    readFileSync,
    writeFileSync,
    openSync,
    readSync,
    fstatSync,
    closeSync,
} from "node:fs";
import { join, resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { repositoryRoot, containedPath } from "./generate-feature.mjs";
import {
    runtimeDirectory,
    processIdentity,
    sourceFingerprint,
} from "./task-runtime.mjs";
import { probeRenderedRuntime } from "./task-runtime-browser.mjs";
import { redactDiagnostic, errorLines } from "./diagnostic-redaction.mjs";

export function serverDiagnosticEvents(value) {
    return String(value)
        .split(/\r?\n/u)
        .filter((line) =>
            /\b(?:error|fail(?:ed|ure)?|exception|timeout|aborterror|ERR|WRN)\b|(?:status(?:Code)?[\s:="]*|HTTP\/\S+.*|HTTP\s+(?:GET|POST|PUT|DELETE|PATCH|OPTIONS|HEAD).*responded\s+|Request finished.*)\b[45]\d{2}\b|agent-diagnostics-proof/iu.test(
                line,
            ),
        )
        .slice(-15)
        .map((line) => redactDiagnostic(line, 1200));
}

export function diagnosticTail(path) {
    if (!existsSync(path)) return "";
    const descriptor = openSync(path, "r");
    try {
        const size = fstatSync(descriptor).size;
        const buffer = Buffer.alloc(Math.min(size, 64_000));
        readSync(
            descriptor,
            buffer,
            0,
            buffer.length,
            Math.max(0, size - buffer.length),
        );
        return buffer.toString("utf8");
    } finally {
        closeSync(descriptor);
    }
}

export function runtimeDiagnosticState(name, root = repositoryRoot) {
    const directory = runtimeDirectory(name, root);
    const state = JSON.parse(
        readFileSync(join(directory, "runtime.json"), "utf8"),
    );
    if (resolve(state.checkout) !== resolve(root))
        throw new Error("Runtime belongs to another checkout");
    const apiUrl = `http://127.0.0.1:${state.ports.api}`;
    const frontendUrl = `http://127.0.0.1:${state.ports.frontend}`;
    if (
        state.apiUrl !== apiUrl ||
        state.frontendUrl !== frontendUrl ||
        !Number.isInteger(state.ports.api) ||
        !Number.isInteger(state.ports.frontend)
    )
        throw new Error("Runtime endpoints do not match owned loopback ports");
    const processes = state.processes.map((process) => ({
        role: process.role,
        alive: processIdentity(process.pid) === process.identity,
    }));
    const browserPath = containedPath(
        root,
        `.artifacts/task-runtime/${name}/browser-diagnostics.json`,
    );
    return {
        schemaVersion: 1,
        name,
        checkout: root,
        apiUrl,
        frontendUrl,
        status: state.status,
        staleSources: state.sourceFingerprint !== sourceFingerprint(root),
        processes,
        browser: existsSync(browserPath)
            ? JSON.parse(readFileSync(browserPath, "utf8"))
            : { events: [], status: "not-observed" },
        server: ["api", "frontend", "initializer"].map((role) => ({
            role,
            events: serverDiagnosticEvents(
                diagnosticTail(
                    containedPath(
                        root,
                        `.artifacts/task-runtime/${name}/${role}.log`,
                    ),
                ),
            ),
        })),
        reproduce: `node scripts/ai/runtime-diagnostics.mjs collect ${name}`,
        stop: `./scripts/Stop-FoodDiaryTask.ps1 -TaskName ${name}`,
    };
}

export async function collectRuntimeDiagnostics(
    name,
    { probeFailure = false, root = repositoryRoot } = {},
) {
    const diagnostics = runtimeDiagnosticState(name, root);
    if (
        !diagnostics.processes.some(
            (process) => process.role === "api" && process.alive,
        ) ||
        !diagnostics.processes.some(
            (process) => process.role === "frontend" && process.alive,
        )
    )
        throw new Error(
            "Owned API/frontend is not running; inspect status before restart",
        );
    const directory = runtimeDirectory(name, root);
    const credentials = JSON.parse(
        readFileSync(
            containedPath(
                root,
                `.artifacts/task-runtime/${name}/credentials.json`,
            ),
            "utf8",
        ),
    );
    const login = await fetch(`${diagnostics.apiUrl}/api/v1/auth/login`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({
            email: credentials.email,
            password: credentials.password,
        }),
        signal: AbortSignal.timeout(10_000),
        redirect: "error",
    });
    if (!login.ok)
        throw new Error(
            `Owned diagnostic authentication failed (${login.status})`,
        );
    const auth = await login.json();
    const user = await fetch(`${diagnostics.apiUrl}/api/v1/users/info`, {
        headers: { authorization: `Bearer ${auth.accessToken}` },
        signal: AbortSignal.timeout(10_000),
        redirect: "error",
    });
    if (!user.ok)
        throw new Error(`Owned diagnostic user probe failed (${user.status})`);
    const identity = await user.json();
    try {
        await probeRenderedRuntime(
            root,
            diagnostics.frontendUrl,
            auth.accessToken,
            identity.id,
            {
                probeFailure,
                onDiagnostics: (events) =>
                    writeFileSync(
                        join(directory, "browser-diagnostics.json"),
                        `${JSON.stringify({ observedAtUtc: new Date().toISOString(), events }, null, 2)}\n`,
                    ),
            },
        );
    } catch (error) {
        diagnostics.collectionError = redactDiagnostic(error.message);
    }
    return {
        ...runtimeDiagnosticState(name, root),
        collectionError: diagnostics.collectionError ?? null,
    };
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    try {
        const [action, name] = process.argv.slice(2);
        if (!["collect", "status", "verify-probe"].includes(action))
            throw new Error(
                "Use collect, status or verify-probe with an owned runtime name",
            );
        console.log(
            JSON.stringify(
                action === "status"
                    ? runtimeDiagnosticState(name)
                    : await collectRuntimeDiagnostics(name, {
                          probeFailure: action === "verify-probe",
                      }),
                null,
                2,
            ),
        );
    } catch (error) {
        console.error(redactDiagnostic(error.message));
        process.exitCode = 1;
    }
}
