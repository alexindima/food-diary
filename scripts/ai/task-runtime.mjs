import { createHash, randomBytes, randomUUID } from "node:crypto";
import { spawn, spawnSync } from "node:child_process";
import {
    closeSync,
    existsSync,
    mkdirSync,
    openSync,
    readFileSync,
    readdirSync,
    unlinkSync,
    writeFileSync,
} from "node:fs";
import { createServer } from "node:net";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { containedPath, repositoryRoot } from "./generate-feature.mjs";
import { probeRenderedRuntime } from "./task-runtime-browser.mjs";
import { recordBuildScope, requireDiskBudget } from "./resource-budget.mjs";
import { prepareTaskFrontendWorkspace } from "./task-frontend-workspace.mjs";

const images = {
    postgres:
        "postgres:17-alpine@sha256:18cfe3ef5e6815560c98237d6216d1e5119702fb0f3894c8785dd58b8bbe5d73",
    redis: "redis:7-alpine@sha256:e7723ff73d963f5cc6d9c4643ea3d989527a402a319239054e9472a7fb9219a2",
    storage:
        "quay.io/minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e",
    storageClient:
        "quay.io/minio/mc@sha256:a7fe349ef4bd8521fb8497f55c6042871b2ae640607cf99d9bede5e9bdf11727",
};

const sleep = (milliseconds) =>
    new Promise((resolveWait) => setTimeout(resolveWait, milliseconds));
const save = (path, value) =>
    writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`, {
        encoding: "utf8",
        mode: 0o600,
    });

function command(file, args, cwd = repositoryRoot) {
    const result = spawnSync(file, args, {
        cwd,
        encoding: "utf8",
        windowsHide: true,
        maxBuffer: 16 * 1024 * 1024,
    });
    if (result.error || result.status !== 0)
        throw new Error(
            `${file} failed (${result.status ?? result.error?.code}); ${result.stderr?.trim() ?? ""}`,
        );
    return result.stdout.trim();
}

export function sourceFingerprint(root = repositoryRoot) {
    const digest = createHash("sha256");
    digest.update(command("git", ["rev-parse", "HEAD"], root));
    const sourceScope = [
        "FoodDiary.Web.Api",
        "FoodDiary.Initializer",
        "FoodDiary.Infrastructure",
        "FoodDiary.ReadModel.Composition",
        "Modules",
        "Shared",
        "FoodDiary.Web.Client/src/app",
        "FoodDiary.Web.Client/src/environments",
        "FoodDiary.Web.Client/projects/fooddiary-admin/src",
        "FoodDiary.Web.Client/projects/fd-webmcp",
        "FoodDiary.Web.Client/angular.json",
        "FoodDiary.Web.Client/package.json",
        "FoodDiary.Web.Client/package-lock.json",
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props",
        "scripts/ai/task-runtime.mjs",
        "scripts/ai/task-frontend-workspace.mjs",
        "scripts/ai/task-frontend-identity.mjs",
        "scripts/ai/task-runtime-browser.mjs",
        "scripts/ai/resource-budget.mjs",
        "scripts/ai/runtime-diagnostics.mjs",
        "scripts/ai/diagnostic-redaction.mjs",
        "scripts/ai/local-provider-stub.mjs",
        ":(exclude)**/tests/**",
        ":(exclude)**/*.spec.ts",
    ];
    digest.update(command("git", ["diff", "HEAD", "--", ...sourceScope], root));
    for (const path of command(
        "git",
        ["ls-files", "--others", "--exclude-standard", "--", ...sourceScope],
        root,
    )
        .split("\n")
        .filter(Boolean)
        .sort()) {
        digest.update(path);
        digest.update(readFileSync(containedPath(root, path)));
    }
    return digest.digest("hex");
}

export function runtimeDirectory(name, root = repositoryRoot) {
    if (!/^[a-z][a-z0-9-]{0,39}$/u.test(name ?? ""))
        throw new Error(
            "Task name must be a lowercase slug of at most 40 characters",
        );
    return containedPath(root, `.artifacts/task-runtime/${name}`);
}

export async function freePorts(count) {
    const listeners = [];
    try {
        for (let index = 0; index < count; index++) {
            const server = createServer();
            await new Promise((resolveListen, reject) => {
                server.once("error", reject);
                server.listen(0, "127.0.0.1", resolveListen);
            });
            listeners.push(server);
        }
        return listeners.map((server) => server.address().port);
    } finally {
        await Promise.all(
            listeners.map(
                (server) =>
                    new Promise((resolveClose) => server.close(resolveClose)),
            ),
        );
    }
}

export function processIdentity(pid) {
    if (process.platform === "win32") {
        const output = command("powershell.exe", [
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            `$taskProcess = Get-Process -Id ${Number(pid)} -ErrorAction SilentlyContinue; if ($taskProcess) { $taskProcess.StartTime.ToUniversalTime().Ticks }; exit 0`,
        ]);
        return output || null;
    }
    try {
        return readFileSync(`/proc/${pid}/stat`, "utf8")
            .split(") ")[1]
            .split(" ")[19];
    } catch {
        return null;
    }
}

export async function listRuntimes(root = repositoryRoot, options = {}) {
    let roots = [root];
    if (options.includeWorktrees !== false) {
        const inventory = command(
            "git",
            ["worktree", "list", "--porcelain"],
            root,
        );
        roots = inventory
            .split("\n")
            .filter((line) => line.startsWith("worktree "))
            .map((line) => line.slice(9));
    }
    const records = [];
    let invalidRecordCount = 0;
    const identities = new Map();
    for (const checkout of roots) {
        const base = join(checkout, ".artifacts", "task-runtime");
        if (!existsSync(base)) continue;
        for (const entry of readdirSync(base, { withFileTypes: true })) {
            if (!entry.isDirectory() || entry.isSymbolicLink()) continue;
            const statePath = join(base, entry.name, "runtime.json");
            if (!existsSync(statePath)) continue;
            try {
                const state = JSON.parse(readFileSync(statePath, "utf8"));
                if (
                    typeof state.checkout !== "string" ||
                    resolve(state.checkout) !== resolve(checkout)
                )
                    throw new Error("Runtime belongs to another checkout");
                if (
                    ![
                        "starting",
                        "ready",
                        "failed",
                        "stopped",
                        "cleanup-failed",
                    ].includes(state.status)
                )
                    throw new Error("Invalid runtime status");
                if (
                    options.includeInactive !== true &&
                    !["starting", "ready", "cleanup-failed"].includes(
                        state.status,
                    )
                )
                    continue;
                const localUrl = (value) => {
                    if (typeof value !== "string") return null;
                    const url = new URL(value);
                    if (
                        url.protocol !== "http:" ||
                        !["localhost", "127.0.0.1", "[::1]"].includes(
                            url.hostname,
                        ) ||
                        url.username ||
                        url.password
                    )
                        return null;
                    return url.origin;
                };
                const frontendUrl = localUrl(state.frontendUrl);
                const apiUrl = localUrl(state.apiUrl);
                const adminUrl = localUrl(state.adminUrl);
                const owners = state.processes ?? [];
                const ownedProcessesAlive =
                    owners.length > 0 &&
                    owners.every(
                        (owner) =>
                            Number.isSafeInteger(owner.pid) &&
                            owner.pid > 0 &&
                            typeof owner.identity === "string" &&
                            owner.identity.length > 0 &&
                            (() => {
                                if (!identities.has(owner.pid))
                                    identities.set(
                                        owner.pid,
                                        processIdentity(owner.pid),
                                    );
                                return (
                                    identities.get(owner.pid) === owner.identity
                                );
                            })(),
                    );
                const probe = async (url) => {
                    if (!url || !ownedProcessesAlive || options.probe === false)
                        return null;
                    try {
                        return (
                            await fetch(url, {
                                signal: AbortSignal.timeout(1000),
                                redirect: "error",
                            })
                        ).ok;
                    } catch {
                        return false;
                    }
                };
                const [frontendResponding, apiReady] = await Promise.all([
                    probe(frontendUrl),
                    probe(apiUrl ? `${apiUrl}/health/ready` : null),
                ]);
                records.push({
                    name: entry.name,
                    owner:
                        typeof state.id === "string" &&
                        /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/iu.test(
                            state.id,
                        )
                            ? state.id
                            : null,
                    checkout,
                    recordedStatus: state.status,
                    observedStatus:
                        state.status === "ready" &&
                        ownedProcessesAlive &&
                        frontendResponding === true &&
                        apiReady === true
                            ? "ready"
                            : state.status === "ready"
                              ? "unverified"
                              : state.status,
                    frontendUrl,
                    apiUrl,
                    adminUrl,
                    ownedProcessesAlive,
                    frontendResponding,
                    apiReady,
                });
            } catch {
                invalidRecordCount++;
                if (options.includeInvalid === true)
                    records.push({
                        name: entry.name,
                        checkout,
                        observedStatus: "invalid-record",
                    });
            }
        }
    }
    return { schemaVersion: 1, runtimes: records, invalidRecordCount };
}

export function stopOwnedProcess(owner) {
    const current = processIdentity(owner.pid);
    if (current === null) return;
    if (current !== owner.identity)
        throw new Error(`Refusing to stop reused PID ${owner.pid}`);
    if (process.platform === "win32")
        command("powershell.exe", [
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            `Stop-Process -Id ${Number(owner.pid)} -ErrorAction Stop`,
        ]);
    else process.kill(-owner.pid, "SIGTERM");
}

function background(file, args, cwd, env, logPath) {
    const log = openSync(logPath, "a", 0o600);
    const child = spawn(file, args, {
        cwd,
        env,
        detached: true,
        windowsHide: true,
        stdio: ["ignore", log, log],
    });
    closeSync(log);
    if (!child.pid) throw new Error("Failed to start owned process");
    child.unref();
    return { pid: child.pid, identity: processIdentity(child.pid), logPath };
}

async function loggedCommand(file, args, cwd, env, logPath) {
    const log = openSync(logPath, "a", 0o600);
    try {
        const child = spawn(file, args, {
            cwd,
            env,
            windowsHide: true,
            stdio: ["ignore", log, log],
        });
        const code = await new Promise((resolveExit, reject) => {
            child.once("error", reject);
            child.once("exit", resolveExit);
        });
        if (code !== 0)
            throw new Error(`${file} failed (${code}); inspect ${logPath}`);
    } finally {
        closeSync(log);
    }
}

async function waitUntil(label, probe, milliseconds = 120_000) {
    const deadline = Date.now() + milliseconds;
    let last = "";
    while (Date.now() < deadline) {
        try {
            if (await probe()) return;
        } catch (error) {
            last = error.message;
        }
        await sleep(500);
    }
    throw new Error(`${label} was not ready${last ? `: ${last}` : ""}`);
}

function containerState(id, field) {
    return command("docker", ["inspect", "--format", field, id]);
}

export function stopRuntime(name, root = repositoryRoot) {
    const directory = runtimeDirectory(name, root);
    const statePath = join(directory, "runtime.json");
    if (!existsSync(statePath)) throw new Error("No owned task runtime exists");
    const state = JSON.parse(readFileSync(statePath, "utf8"));
    if (state.checkout !== resolve(root) || state.directory !== directory)
        throw new Error("Runtime belongs to another checkout");
    const failures = [];
    for (const owner of [...state.processes].reverse()) {
        try {
            stopOwnedProcess(owner);
        } catch (error) {
            failures.push(error.message);
        }
    }
    for (const owner of [...state.containers].reverse()) {
        try {
            const inspect = spawnSync(
                "docker",
                [
                    "inspect",
                    "--format",
                    '{{ index .Config.Labels "fooddiary.task" }}',
                    owner.id,
                ],
                { encoding: "utf8", windowsHide: true },
            );
            if (inspect.status !== 0) continue;
            if (inspect.stdout.trim() !== state.id)
                throw new Error(
                    `Refusing to remove foreign container ${owner.name}`,
                );
            command("docker", ["rm", "-f", owner.id]);
        } catch (error) {
            failures.push(error.message);
        }
    }
    state.status = failures.length ? "cleanup-failed" : "stopped";
    state.stoppedAtUtc = new Date().toISOString();
    state.failures = failures;
    save(statePath, state);
    if (failures.length) throw new Error(failures.join("; "));
    return state;
}

export function safeSettings(root, directory, ports, id) {
    const settings = JSON.parse(
        readFileSync(join(root, "FoodDiary.Web.Api/appsettings.json"), "utf8"),
    );
    const blankCredentials = (value) => {
        if (value === null || typeof value !== "object") return;
        for (const [key, child] of Object.entries(value)) {
            if (
                typeof child === "string" &&
                /(ApiKey$|SecretKey$|PrivateKey$|PublicKey$|Password$|Token$|^ClientId$|^ClientSecret$|^AccessKeyId$|^SecretAccessKey$|^Username$|^ShopId$|^MerchantId$)/iu.test(
                    key,
                )
            )
                value[key] = "";
            else blankCredentials(child);
        }
    };
    blankCredentials(settings);
    const password = randomBytes(24).toString("base64url");
    const storageUser = `task${id.replaceAll("-", "").slice(0, 12)}`;
    const storagePassword = randomBytes(24).toString("base64url");
    const email = `task-${id}@example.test`;
    const accountPassword = `Task!${randomBytes(24).toString("base64url")}`;
    const apiUrl = `http://127.0.0.1:${ports.api}`;
    const frontendUrl = `http://127.0.0.1:${ports.frontend}`;
    settings.urls = apiUrl;
    settings.ConnectionStrings = {
        DefaultConnection: `Host=127.0.0.1;Port=${ports.postgres};Database=fooddiary_task;Username=postgres;Password=${password};Include Error Detail=false`,
        Redis: `127.0.0.1:${ports.redis}`,
    };
    settings.Jwt.SecretKey = randomBytes(48).toString("base64url");
    settings.Jwt.Issuer = apiUrl;
    settings.Jwt.Audience = frontendUrl;
    settings.Cors = { Origins: [frontendUrl] };
    settings.HttpsRedirection = { Enabled: false };
    settings.DataProtection = {
        ApplicationName: `FoodDiary-Task-${id}`,
        KeyRingPath: join(directory, "keys"),
    };
    settings.InitialAdmin = {
        Email: email,
        Password: accountPassword,
        BootstrapTimeoutSeconds: 120,
    };
    settings.OpenAi.ApiKey = "";
    settings.TelegramAuth = {};
    settings.TelegramBot = {};
    settings.GoogleAuth = {};
    settings.WebPush = {};
    settings.OpenTelemetry = { Otlp: { Endpoint: "" } };
    settings.Email.FrontendBaseUrl = frontendUrl;
    settings.Email.AllowedFrontendBaseUrls = [frontendUrl];
    settings.Email.FromAddress = "task@example.test";
    settings.MailRelayClient = {
        BaseUrl: `http://127.0.0.1:${ports.providers}`,
        ApiKey: randomBytes(24).toString("hex"),
        AllowInsecureHttp: true,
    };
    settings.MailInboxClient = {
        BaseUrl: `http://127.0.0.1:${ports.providers}`,
        MetadataApiKey: randomBytes(24).toString("hex"),
        ContentApiKey: randomBytes(24).toString("hex"),
        StateApiKey: randomBytes(24).toString("hex"),
        AllowInsecureLoopback: true,
    };
    settings.S3 = {
        ...settings.S3,
        AccessKeyId: storageUser,
        SecretAccessKey: storagePassword,
        Region: "us-east-1",
        Bucket: "fooddiary-public",
        StagingBucket: "fooddiary-staging",
        ServiceUrl: `http://127.0.0.1:${ports.storage}`,
        AllowInsecureHttp: true,
        AllowPublicImageAccess: true,
        PublicBaseUrl: `http://127.0.0.1:${ports.storage}/fooddiary-public`,
    };
    save(join(directory, "appsettings.json"), settings);
    save(join(directory, "credentials.json"), {
        email,
        password: accountPassword,
    });
    writeFileSync(
        join(directory, "postgres.env"),
        `POSTGRES_DB=fooddiary_task\nPOSTGRES_USER=postgres\nPOSTGRES_PASSWORD=${password}\n`,
        { mode: 0o600 },
    );
    writeFileSync(
        join(directory, "storage.env"),
        `MINIO_ROOT_USER=${storageUser}\nMINIO_ROOT_PASSWORD=${storagePassword}\nMINIO_API_CORS_ALLOW_ORIGIN=${frontendUrl}\n`,
        { mode: 0o600 },
    );
    return { apiUrl, frontendUrl, email, accountPassword };
}

export async function startRuntime(name, root = repositoryRoot) {
    root = resolve(root);
    requireDiskBudget(root);
    const directory = runtimeDirectory(name, root);
    mkdirSync(directory, { recursive: true });
    const statePath = join(directory, "runtime.json");
    if (
        existsSync(statePath) &&
        !["stopped", "failed"].includes(
            JSON.parse(readFileSync(statePath, "utf8")).status,
        )
    )
        throw new Error(
            "Task runtime already exists; inspect or stop it before starting again",
        );
    const head = command("git", ["rev-parse", "HEAD"], root);
    const fingerprint = sourceFingerprint(root);
    const lockPath = join(directory, "start.lock");
    const lock = openSync(lockPath, "wx", 0o600);
    const state = {
        schemaVersion: 1,
        id: randomUUID(),
        name,
        checkout: root,
        directory,
        head,
        sourceFingerprint: fingerprint,
        status: "starting",
        startedAtUtc: new Date().toISOString(),
        containers: [],
        processes: [],
        integrations: {
            database: "pending",
            cache: "pending",
            storage: "pending",
            ai: "disabled; no provider key; external HTTP blocked",
            email: "disabled; local provider stub returns 501",
            payments: "disabled; no provider credentials",
            backgroundJobs: "not started",
        },
    };
    save(statePath, state);
    recordBuildScope(state, root);
    const persist = () => save(statePath, state);
    try {
        command("docker", ["info", "--format", "{{.ServerVersion}}"]);
        const selected = await freePorts(6);
        state.ports = Object.fromEntries(
            [
                "api",
                "frontend",
                "postgres",
                "redis",
                "storage",
                "providers",
            ].map((key, index) => [key, selected[index]]),
        );
        const configuration = safeSettings(
            root,
            directory,
            state.ports,
            state.id,
        );
        state.apiUrl = configuration.apiUrl;
        state.frontendUrl = configuration.frontendUrl;
        state.credentialsPath = join(directory, "credentials.json");
        state.stopCommand = `node scripts/ai/task-runtime.mjs stop --name ${name}`;
        persist();
        const createContainer = (role, image, args) => {
            const containerName = `fd-task-${state.id.slice(0, 8)}-${role}`;
            const id = command("docker", [
                "run",
                "-d",
                "--name",
                containerName,
                "--label",
                `fooddiary.task=${state.id}`,
                "--label",
                `fooddiary.checkout=${createHash("sha256").update(root).digest("hex")}`,
                ...args,
                image,
                ...(role === "storage"
                    ? ["server", "/data", "--address", ":9000"]
                    : []),
            ]);
            state.containers.push({ role, name: containerName, id });
            persist();
            return id;
        };
        console.log("Preparing owned PostgreSQL, Redis and storage");
        const postgres = createContainer("postgres", images.postgres, [
            "--env-file",
            join(directory, "postgres.env"),
            "-p",
            `127.0.0.1:${state.ports.postgres}:5432`,
        ]);
        createContainer("redis", images.redis, [
            "-p",
            `127.0.0.1:${state.ports.redis}:6379`,
        ]);
        const storage = createContainer("storage", images.storage, [
            "--env-file",
            join(directory, "storage.env"),
            "-p",
            `127.0.0.1:${state.ports.storage}:9000`,
        ]);
        await waitUntil(
            "PostgreSQL",
            () =>
                spawnSync(
                    "docker",
                    [
                        "exec",
                        postgres,
                        "pg_isready",
                        "-U",
                        "postgres",
                        "-d",
                        "fooddiary_task",
                    ],
                    { windowsHide: true },
                ).status === 0,
        );
        await waitUntil(
            "Storage",
            async () =>
                (
                    await fetch(
                        `http://127.0.0.1:${state.ports.storage}/minio/health/ready`,
                        {
                            signal: AbortSignal.timeout(3000),
                            redirect: "error",
                        },
                    )
                ).ok,
        );
        const initializeStorage =
            'mc alias set local http://127.0.0.1:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null && mc mb --ignore-existing local/fooddiary-public >/dev/null && mc mb --ignore-existing local/fooddiary-staging >/dev/null && mc anonymous set download local/fooddiary-public >/dev/null';
        command("docker", [
            "run",
            "--rm",
            "--network",
            `container:${storage}`,
            "--env-file",
            join(directory, "storage.env"),
            "--entrypoint",
            "/bin/sh",
            images.storageClient,
            "-c",
            initializeStorage,
        ]);
        state.integrations.database = "owned local PostgreSQL ready";
        state.integrations.cache = "owned local Redis";
        state.integrations.storage =
            "owned local S3-compatible storage and two initialized buckets";
        persist();
        const env = {
            ...process.env,
            ASPNETCORE_ENVIRONMENT: "Development",
            DOTNET_ENVIRONMENT: "Development",
            FOODDIARY_TASK_CONFIG: join(directory, "appsettings.json"),
        };
        for (const key of Object.keys(env))
            if (
                /^(FOODDIARY_|ASPNETCORE_|DOTNET_STARTUP_HOOKS)/u.test(key) &&
                !["FOODDIARY_TASK_CONFIG", "ASPNETCORE_ENVIRONMENT"].includes(
                    key,
                )
            )
                delete env[key];
        const build = join(directory, "build");
        console.log(
            "Building API and initializer in the task-owned output directory",
        );
        await loggedCommand(
            "dotnet",
            [
                "build",
                "FoodDiary.Web.Api/FoodDiary.Web.Api.csproj",
                "--artifacts-path",
                build,
                "-m:2",
            ],
            root,
            env,
            join(directory, "build.log"),
        );
        await loggedCommand(
            "dotnet",
            [
                "build",
                "FoodDiary.Initializer/FoodDiary.Initializer.csproj",
                "--artifacts-path",
                build,
                "-m:2",
            ],
            root,
            env,
            join(directory, "build.log"),
        );
        await loggedCommand(
            "dotnet",
            [
                join(
                    build,
                    "bin/FoodDiary.Initializer/debug/FoodDiary.Initializer.dll",
                ),
                "update",
            ],
            root,
            env,
            join(directory, "initializer.log"),
        );
        const providers = background(
            process.execPath,
            [
                join(root, "scripts/ai/local-provider-stub.mjs"),
                String(state.ports.providers),
                state.id,
            ],
            root,
            env,
            join(directory, "providers.log"),
        );
        state.processes.push({ role: "providers", ...providers });
        persist();
        const api = background(
            "dotnet",
            [join(build, "bin/FoodDiary.Web.Api/debug/FoodDiary.Web.Api.dll")],
            root,
            env,
            join(directory, "api.log"),
        );
        state.processes.push({ role: "api", ...api });
        persist();
        await waitUntil(
            "API",
            async () =>
                (
                    await fetch(`${configuration.apiUrl}/health/ready`, {
                        signal: AbortSignal.timeout(3000),
                        redirect: "error",
                    })
                ).ok,
        );
        const login = await fetch(`${configuration.apiUrl}/api/v1/auth/login`, {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({
                email: configuration.email,
                password: configuration.accountPassword,
            }),
            redirect: "error",
        });
        if (!login.ok)
            throw new Error(`Owned account login failed (${login.status})`);
        const auth = await login.json();
        if (typeof auth.accessToken !== "string")
            throw new Error("Owned account login has no token");
        const user = await fetch(`${configuration.apiUrl}/api/v1/users/info`, {
            headers: { authorization: `Bearer ${auth.accessToken}` },
            redirect: "error",
        });
        if (!user.ok)
            throw new Error(`Owned user API probe failed (${user.status})`);
        state.seed = {
            account: "synthetic local account",
            authenticatedApi: user.status,
        };
        console.log("Building frontend and starting its task-owned API proxy");
        const client = join(root, "FoodDiary.Web.Client");
        const workspace = prepareTaskFrontendWorkspace(root, directory, { frontendUrl: configuration.frontendUrl });
        state.frontendWorkspace = workspace;
        await loggedCommand(
            process.execPath,
            [
                join(client, "node_modules/@angular/cli/bin/ng.js"),
                "build",
                "food-diary-web-client",
                "--configuration=task",
                "--output-path",
                join(directory, "frontend-build"),
            ],
            workspace,
            env,
            join(directory, "frontend-build.log"),
        );
        save(join(directory, "proxy.json"), {
            "/api": {
                target: configuration.apiUrl,
                secure: false,
                changeOrigin: false,
            },
            "/hubs": { target: configuration.apiUrl, secure: false, ws: true },
        });
        const frontend = background(
            process.execPath,
            [
                join(client, "node_modules/@angular/cli/bin/ng.js"),
                "serve",
                "food-diary-web-client",
                "--configuration=task",
                "--host=127.0.0.1",
                `--port=${state.ports.frontend}`,
                "--proxy-config",
                join(directory, "proxy.json"),
            ],
            workspace,
            env,
            join(directory, "frontend.log"),
        );
        state.processes.push({ role: "frontend", ...frontend });
        ensureFrontendIdentity(state, root, directory);
        persist();
        await waitUntil(
            "Frontend",
            async () =>
                (
                    await fetch(configuration.frontendUrl, {
                        signal: AbortSignal.timeout(3000),
                        redirect: "error",
                    })
                ).ok,
            180_000,
        );
        const proxied = await fetch(
            `${configuration.frontendUrl}/api/v1/users/info`,
            {
                headers: { authorization: `Bearer ${auth.accessToken}` },
                redirect: "error",
            },
        );
        if (!proxied.ok)
            throw new Error(`Frontend-to-API probe failed (${proxied.status})`);
        const ownedUser = await user.json();
        state.browserProbe = await probeRenderedRuntime(
            root,
            configuration.frontendUrl,
            auth.accessToken,
            ownedUser.id,
            {
                onDiagnostics: (events) =>
                    save(join(directory, "browser-diagnostics.json"), {
                        observedAtUtc: new Date().toISOString(),
                        events,
                    }),
            },
        );
        if (sourceFingerprint(root) !== state.sourceFingerprint)
            throw new Error(
                "Sources changed during runtime preparation; rebuild this task runtime",
            );
        state.status = "ready";
        state.readyAtUtc = new Date().toISOString();
        state.proxyProbeStatus = proxied.status;
        persist();
        return state;
    } catch (error) {
        state.failure = error.message;
        persist();
        try {
            stopRuntime(name, root);
        } catch (cleanup) {
            state.cleanupFailure = cleanup.message;
        }
        state.status = "failed";
        persist();
        throw error;
    } finally {
        closeSync(lock);
        unlinkSync(lockPath);
    }
}

/** Add the admin UI to one existing owned runtime, retaining its API and data. */
function ensureFrontendIdentity(state, root, directory) {
    const existing = state.processes.find(owner => owner.role === "frontend-identity");
    if (existing) {
        if (processIdentity(existing.pid) !== existing.identity) throw new Error("Frontend identity watcher needs owned runtime recovery");
        return;
    }
    state.processes.push({ role: "frontend-identity", ...background(process.execPath, [join(root, "scripts/ai/task-frontend-identity.mjs"), "watch", directory], root, process.env, join(directory, "frontend-identity.log")) });
}

export async function startAdminRuntime(name, root = repositoryRoot) {
    root = resolve(root);
    const directory = runtimeDirectory(name, root);
    const statePath = join(directory, "runtime.json");
    const state = JSON.parse(readFileSync(statePath, "utf8"));
    if (resolve(state.checkout) !== root || state.status !== "ready")
        throw new Error(
            "Admin requires a ready runtime belonging to this checkout",
        );
    const frontend = state.processes.find((owner) => owner.role === "frontend");
    if (!frontend || processIdentity(frontend.pid) !== frontend.identity)
        throw new Error(
            "Owned frontend process is unavailable or its PID was reused",
        );
    const admin = state.processes.find((owner) => owner.role === "admin");
    if (
        admin &&
        processIdentity(admin.pid) === admin.identity &&
        (
            await fetch(state.adminUrl, {
                signal: AbortSignal.timeout(3000),
                redirect: "error",
            })
        ).ok
    ) {
        if (state.frontendWorkspace === join(directory, "frontend-workspace")) {
            prepareTaskFrontendWorkspace(root, directory, { frontendUrl: state.frontendUrl, adminUrl: state.adminUrl });
            ensureFrontendIdentity(state, root, directory);
            save(statePath, state);
        }
        return state;
    }
    if (admin)
        throw new Error("Recorded admin process needs owned runtime recovery");
    const lockPath = join(directory, "admin-start.lock");
    const lock = openSync(lockPath, "wx", 0o600);
    const client = join(root, "FoodDiary.Web.Client");
    const previousWorkspace = state.frontendWorkspace ?? client;
    const previousOwner = { ...frontend };
    const created = [];
    let frontendStopped = false;
    const serve = (project, workspace, port, logPath) =>
        background(
            process.execPath,
            [
                join(client, "node_modules/@angular/cli/bin/ng.js"),
                "serve",
                project,
                "--configuration=task",
                "--host=127.0.0.1",
                `--port=${port}`,
                "--proxy-config",
                join(directory, "proxy.json"),
            ],
            workspace,
            process.env,
            logPath,
        );
    try {
        const [adminPort] = await freePorts(1);
        const adminUrl = `http://127.0.0.1:${adminPort}`;
        const workspace = prepareTaskFrontendWorkspace(root, directory, {
            frontendUrl: state.frontendUrl,
            adminUrl,
        });
        for (const project of ["food-diary-web-client", "fooddiary-admin"]) {
            console.log(`Building task-owned ${project}`);
            await loggedCommand(
                process.execPath,
                [
                    join(client, "node_modules/@angular/cli/bin/ng.js"),
                    "build",
                    project,
                    "--configuration=task",
                ],
                workspace,
                process.env,
                join(directory, `${project}-admin-start-build.log`),
            );
        }
        const adminOwner = {
            role: "admin",
            ...serve(
                "fooddiary-admin",
                workspace,
                adminPort,
                join(directory, "admin.log"),
            ),
        };
        created.push(adminOwner);
        state.processes.push(adminOwner);
        state.adminUrl = adminUrl;
        state.ports.admin = adminPort;
        state.admin = { status: "starting", browserSso: "unverified" };
        save(statePath, state);
        await waitUntil(
            "Admin frontend",
            async () =>
                (
                    await fetch(adminUrl, {
                        signal: AbortSignal.timeout(3000),
                        redirect: "error",
                    })
                ).ok,
            180_000,
        );
        stopOwnedProcess(previousOwner);
        frontendStopped = true;
        const replacement = serve(
            "food-diary-web-client",
            workspace,
            state.ports.frontend,
            frontend.logPath,
        );
        created.push(replacement);
        Object.assign(frontend, replacement);
        state.frontendWorkspace = workspace;
        save(statePath, state);
        await waitUntil(
            "Client with admin launcher",
            async () =>
                (
                    await fetch(state.frontendUrl, {
                        signal: AbortSignal.timeout(3000),
                        redirect: "error",
                    })
                ).ok,
            180_000,
        );
        state.admin.status = "frontend-ready";
        state.admin.readyAtUtc = new Date().toISOString();
        ensureFrontendIdentity(state, root, directory);
        save(statePath, state);
        return state;
    } catch (error) {
        for (const owner of created.reverse()) stopOwnedProcess(owner);
        state.processes = state.processes.filter(
            (owner) => owner.role !== "admin",
        );
        if (frontendStopped)
            Object.assign(
                frontend,
                serve(
                    "food-diary-web-client",
                    previousWorkspace,
                    state.ports.frontend,
                    frontend.logPath,
                ),
            );
        if (previousWorkspace === client) delete state.frontendWorkspace;
        else state.frontendWorkspace = previousWorkspace;
        delete state.adminUrl;
        delete state.ports.admin;
        state.admin = { status: "failed", browserSso: "unverified" };
        save(statePath, state);
        throw error;
    } finally {
        closeSync(lock);
        unlinkSync(lockPath);
    }
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    try {
        const [action, option, name] = process.argv.slice(2);
        if (action === "list" && process.argv.length === 3) {
            console.log(JSON.stringify(await listRuntimes(), null, 2));
        } else {
            if (
                option !== "--name" ||
                process.argv.length !== 5 ||
                !["start", "stop", "status", "admin"].includes(action)
            )
                throw new Error(
                    "Use task-runtime.mjs list or start|stop|status|admin --name <task-name>",
                );
            const state =
                action === "start"
                    ? await startRuntime(name)
                    : action === "admin"
                      ? await startAdminRuntime(name)
                      : action === "stop"
                        ? stopRuntime(name)
                        : JSON.parse(
                              readFileSync(
                                  join(runtimeDirectory(name), "runtime.json"),
                                  "utf8",
                              ),
                          );
            console.log(JSON.stringify(state, null, 2));
        }
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
