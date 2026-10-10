import { spawnSync } from "node:child_process";
import {
    existsSync,
    mkdirSync,
    readFileSync,
    realpathSync,
    statfsSync,
    writeFileSync,
} from "node:fs";
import { join, resolve, sep } from "node:path";
import { pathToFileURL } from "node:url";
import { repositoryRoot } from "./generate-feature.mjs";

export function diskBudget(root, minimumGiB = 3, inspect = statfsSync) {
    if (!Number.isFinite(minimumGiB) || minimumGiB <= 0)
        throw new Error("Invalid disk budget");
    const stats = inspect(root);
    const availableBytes = Number(stats.bavail) * Number(stats.bsize);
    if (!Number.isFinite(availableBytes) || availableBytes < 0)
        throw new Error("Disk space could not be measured");
    return {
        availableGiB: availableBytes / 1024 ** 3,
        minimumGiB,
        ready: availableBytes >= minimumGiB * 1024 ** 3,
    };
}

export function requireDiskBudget(root = repositoryRoot, minimumGiB = 3) {
    const result = diskBudget(root, minimumGiB);
    if (!result.ready)
        throw new Error(
            `Insufficient workspace disk space: ${result.availableGiB.toFixed(2)} GiB available; ${minimumGiB} GiB required. Clean only stopped, owned build scopes.`,
        );
    return result;
}

export function ownedBuildScope(state, root = repositoryRoot) {
    if (
        !/^[a-z][a-z0-9-]{0,39}$/u.test(state.name ?? "") ||
        !/^[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}$/u.test(state.id ?? "")
    )
        throw new Error("Invalid runtime identity");
    const directory = resolve(root, ".artifacts/task-runtime", state.name);
    if (
        resolve(state.checkout) !== resolve(root) ||
        resolve(state.directory) !== directory
    )
        throw new Error("Foreign build scope");
    const build = join(directory, "build");
    if (existsSync(build)) {
        const actualRoot = realpathSync(root);
        const actual = realpathSync(build);
        const normalize = (value) =>
            process.platform === "win32" ? value.toLowerCase() : value;
        if (!normalize(actual).startsWith(normalize(actualRoot + sep)))
            throw new Error("Build scope escapes the checkout");
    }
    return {
        schemaVersion: 1,
        ownerId: state.id,
        name: state.name,
        checkout: resolve(root),
        build,
        projects: [
            "FoodDiary.Web.Api/FoodDiary.Web.Api.csproj",
            "FoodDiary.Initializer/FoodDiary.Initializer.csproj",
        ],
    };
}

export function recordBuildScope(state, root = repositoryRoot) {
    const scope = ownedBuildScope(state, root);
    const directory = join(root, ".artifacts/resource-scopes");
    mkdirSync(directory, { recursive: true });
    writeFileSync(
        join(directory, `${state.id}.json`),
        JSON.stringify({ ...scope, startedAtUtc: new Date().toISOString() }),
        { mode: 0o600 },
    );
    return scope;
}

export function cleanStoppedBuild(
    state,
    root = repositoryRoot,
    execute = spawnSync,
) {
    if (state.status !== "stopped" || state.failures?.length)
        throw new Error("Build cleanup requires a fully stopped runtime");
    for (const owner of state.processes ?? []) {
        try {
            process.kill(owner.pid, 0);
        } catch {
            continue;
        }
        throw new Error(
            "A recorded runtime process is still alive; build cleanup refused",
        );
    }
    const scope = ownedBuildScope(state, root);
    const receiptPath = join(
        root,
        ".artifacts/resource-scopes",
        `${state.id}.json`,
    );
    if (!existsSync(receiptPath))
        throw new Error(
            "Build scope has no ownership receipt; inspect it manually",
        );
    const receipt = JSON.parse(readFileSync(receiptPath, "utf8"));
    if (
        receipt.ownerId !== scope.ownerId ||
        receipt.build !== scope.build ||
        receipt.checkout !== scope.checkout
    )
        throw new Error("Build ownership receipt does not match");
    for (const project of scope.projects) {
        const result = execute(
            "dotnet",
            [
                "clean",
                project,
                "--artifacts-path",
                scope.build,
                "--verbosity",
                "quiet",
                "-m:2",
            ],
            {
                cwd: root,
                windowsHide: true,
                encoding: "utf8",
                timeout: 120_000,
            },
        );
        if (result.error || result.status !== 0)
            throw new Error(
                "Owned dotnet clean failed; outputs and evidence retained",
            );
    }
    const result = {
        ...receipt,
        cleanedAtUtc: new Date().toISOString(),
        method: "native dotnet clean; reports and runtime credentials retained",
    };
    writeFileSync(receiptPath, JSON.stringify(result), { mode: 0o600 });
    return result;
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    const [action, name] = process.argv.slice(2);
    if (action === "check") console.log(JSON.stringify(requireDiskBudget()));
    else if (
        action === "clean-build" &&
        /^[a-z][a-z0-9-]{0,39}$/u.test(name ?? "")
    ) {
        const state = JSON.parse(
            readFileSync(
                join(
                    repositoryRoot,
                    ".artifacts/task-runtime",
                    name,
                    "runtime.json",
                ),
                "utf8",
            ),
        );
        console.log(JSON.stringify(cleanStoppedBuild(state)));
    } else throw new Error("Use check or clean-build <stopped-owned-runtime>");
}
