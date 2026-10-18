import {
    existsSync,
    mkdirSync,
    readFileSync,
    realpathSync,
    symlinkSync,
    writeFileSync,
} from "node:fs";
import { isAbsolute, join, relative, resolve, sep } from "node:path";
import { writeFrontendIdentity } from "./task-frontend-identity.mjs";

function localOrigin(value) {
    const url = new URL(value);
    if (
        url.protocol !== "http:" ||
        !["127.0.0.1", "localhost", "[::1]"].includes(url.hostname) ||
        url.username ||
        url.password ||
        url.search ||
        url.hash ||
        url.pathname !== "/"
    )
        throw new Error(
            "Task frontend URLs must be plain loopback HTTP origins",
        );
    return url.origin;
}

/** Generate isolated public configuration; never edit the shared Angular workspace. */
export function prepareTaskFrontendWorkspace(root, directory, urls) {
    root = resolve(root);
    directory = resolve(directory);
    const scope = relative(join(root, ".artifacts/task-runtime"), directory);
    if (!/^[a-z0-9][a-z0-9-]{0,63}$/u.test(scope) || isAbsolute(scope))
        throw new Error("Frontend workspace must belong to one task runtime");
    const frontendUrl = localOrigin(urls.frontendUrl);
    const adminUrl = urls.adminUrl === undefined ? null : localOrigin(urls.adminUrl);
    if (frontendUrl === adminUrl)
        throw new Error("Client and admin require separate storage origins");
    const client = join(root, "FoodDiary.Web.Client");
    const workspace = join(directory, "frontend-workspace");
    mkdirSync(workspace, { recursive: true });
    const modules = join(workspace, "node_modules");
    const sourceModules = join(client, "node_modules");
    if (existsSync(modules)) {
        if (realpathSync(modules) !== realpathSync(sourceModules))
            throw new Error(
                "Unexpected dependency directory in owned frontend workspace",
            );
    } else {
        symlinkSync(
            sourceModules,
            modules,
            process.platform === "win32" ? "junction" : "dir",
        );
    }
    const sourceLink = join(workspace, "source");
    if (existsSync(sourceLink)) {
        if (realpathSync(sourceLink) !== realpathSync(client))
            throw new Error(
                "Unexpected source directory in owned frontend workspace",
            );
    } else {
        symlinkSync(
            client,
            sourceLink,
            process.platform === "win32" ? "junction" : "dir",
        );
    }
    const source = JSON.parse(
        readFileSync(join(client, "angular.json"), "utf8"),
    );
    const fromWorkspace = (path) =>
        relative(workspace, path).split(sep).join("/");
    const fromClient = (path) => fromWorkspace(join(client, path));
    const mainEnvironment = join(workspace, "environment.client.ts");
    const adminEnvironment = join(workspace, "environment.admin.ts");
    const taskEnvironmentImport = fromClient(
        "src/environments/environment.task",
    );
    writeFileSync(
        mainEnvironment,
        `import { environment as task } from ${JSON.stringify(taskEnvironmentImport)};\nimport { developmentBuild } from ${JSON.stringify(join(workspace, "frontend-build").split(sep).join("/"))};\nexport const environment = { ...task, developmentBuild${adminUrl === null ? '' : ', adminAppUrl: ' + JSON.stringify(adminUrl)} };\n`,
    );
    writeFileSync(
        adminEnvironment,
        `export const environment = { apiUrls: { auth: '/api/v1/auth' }, mainAppUrl: ${JSON.stringify(frontendUrl)}, enableGlobalErrorHandler: false };\n`,
    );
    const projects = {};
    for (const name of ["food-diary-web-client", "fooddiary-admin"]) {
        const original = source.projects[name];
        const options = structuredClone(original.architect.build.options);
        for (const key of ["browser", "server", "tsConfig"]) {
            if (typeof options[key] === "string")
                options[key] = fromClient(options[key]);
        }
        if (typeof options.index === "string")
            options.index = fromClient(options.index);
        if (typeof options.ssr === "object" && options.ssr.entry)
            options.ssr.entry = fromClient(options.ssr.entry);
        for (const key of ["styles", "scripts", "polyfills"]) {
            options[key] = (options[key] ?? []).map((item) => {
                if (typeof item === "string")
                    return item.endsWith(".js") ||
                        item.endsWith(".ts") ||
                        key !== "polyfills"
                        ? fromClient(item)
                        : item;
                return { ...item, input: fromClient(item.input) };
            });
        }
        options.assets = (options.assets ?? []).map((item) =>
            typeof item === "string"
                ? `source/${item}`
                : { ...item, input: `source/${item.input}` },
        );
        if (options.stylePreprocessorOptions?.includePaths)
            options.stylePreprocessorOptions.includePaths =
                options.stylePreprocessorOptions.includePaths.map(fromClient);
        options.outputPath = {
            ...options.outputPath,
            base: fromWorkspace(
                join(
                    directory,
                    name === "fooddiary-admin"
                        ? "admin-build"
                        : "frontend-build",
                ),
            ),
        };
        const isAdmin = name === "fooddiary-admin";
        projects[name] = {
            projectType: "application",
            root: fromClient(original.root),
            sourceRoot: fromClient(original.sourceRoot),
            architect: {
                build: {
                    builder: original.architect.build.builder,
                    options,
                    configurations: {
                        task: {
                            optimization: false,
                            extractLicenses: false,
                            sourceMap: true,
                            fileReplacements: [
                                {
                                    replace: fromClient(
                                        isAdmin
                                            ? "projects/fooddiary-admin/src/environments/environment.ts"
                                            : "src/environments/environment.ts",
                                    ),
                                    with: fromWorkspace(
                                        isAdmin
                                            ? adminEnvironment
                                            : mainEnvironment,
                                    ),
                                },
                            ],
                        },
                    },
                    defaultConfiguration: "task",
                },
                serve: {
                    builder: original.architect.serve.builder,
                    configurations: {
                        task: { buildTarget: `${name}:build:task` },
                    },
                    defaultConfiguration: "task",
                },
            },
        };
    }
    writeFileSync(
        join(workspace, "angular.json"),
        JSON.stringify(
            {
                version: 1,
                cli: { cache: { path: join(directory, "angular-cache") } },
                projects,
            },
            null,
            2,
        ),
    );
    writeFrontendIdentity(root, workspace);
    return workspace;
}
