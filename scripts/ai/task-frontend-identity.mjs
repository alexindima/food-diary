import { createHash } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync, readdirSync, renameSync, watch, writeFileSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const SOURCE_DIRECTORIES = ["src", "projects", "assets", "public"];
const IGNORED_DIRECTORIES = new Set(["node_modules", "dist", ".angular", ".artifacts", "coverage", "out-tsc"]);

export function writeFrontendIdentity(root, workspace) {
    root = resolve(root);
    workspace = resolve(workspace);
    const scope = relative(join(root, ".artifacts/task-runtime"), workspace).split(sep).join("/");
    if (!/^[a-z0-9][a-z0-9-]{0,63}\/frontend-workspace$/u.test(scope)) throw new Error("Frontend identity requires an owned workspace");
    const client = join(root, "FoodDiary.Web.Client");
    const hash = createHash("sha256");
    const add = (path) => {
        hash.update(relative(root, path).split(sep).join("/"));
        hash.update("\0");
        hash.update(readFileSync(path));
    };
    const walk = (directory) => {
        for (const entry of readdirSync(directory, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name, "en"))) {
            const path = join(directory, entry.name);
            if (entry.isDirectory() && !IGNORED_DIRECTORIES.has(entry.name)) walk(path);
            else if (entry.isFile() && !entry.name.endsWith(".spec.ts")) add(path);
        }
    };
    for (const name of SOURCE_DIRECTORIES) if (existsSync(join(client, name))) walk(join(client, name));
    for (const entry of readdirSync(client).sort()) {
        if (/^(?:angular|package(?:-lock)?|tsconfig(?:\.[\w-]+)?)\.json$/u.test(entry)) add(join(client, entry));
    }
    for (const name of ["angular.json", "environment.client.ts", "environment.admin.ts"]) {
        if (existsSync(join(workspace, name))) add(join(workspace, name));
    }
    const revisionResult = spawnSync("git", ["rev-parse", "HEAD"], { cwd: root, encoding: "utf8", windowsHide: true });
    const candidate = revisionResult.stdout?.trim();
    const revision = /^[a-f\d]{40}$/u.test(candidate ?? "") ? candidate : null;
    const sourceFingerprint = hash.digest("hex");
    const path = join(workspace, "frontend-build.ts");
    const marker = "export const developmentBuild = ";
    if (existsSync(path)) {
        const previous = JSON.parse(readFileSync(path, "utf8").slice(marker.length).trim().replace(/;$/u, ""));
        if (previous.revision === revision && previous.sourceFingerprint === sourceFingerprint) return previous;
    }
    const metadata = { revision, sourceFingerprint, capturedAtUtc: new Date().toISOString(), fingerprintBasis: "frontend-source-and-task-configuration" };
    const temporary = path + `.${process.pid}.tmp`;
    writeFileSync(temporary, marker + JSON.stringify(metadata) + ";\n");
    renameSync(temporary, path);
    return metadata;
}

export function watchFrontendIdentity(root, workspace, onUpdate = () => {}) {
    writeFrontendIdentity(root, workspace);
    let timer;
    const refresh = () => {
        clearTimeout(timer);
        timer = setTimeout(() => {
            try { onUpdate(writeFrontendIdentity(root, workspace)); }
            catch { /* A file may be moving; the next filesystem event retries the capture. */ }
        }, 250);
    };
    const client = join(root, "FoodDiary.Web.Client");
    const watchers = [watch(client, refresh), ...SOURCE_DIRECTORIES.filter(name => existsSync(join(client, name))).map(name => watch(join(client, name), { recursive: true }, refresh))];
    return () => { clearTimeout(timer); for (const watcher of watchers) watcher.close(); };
}

if (process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url) {
    const root = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
    const [action, directory] = process.argv.slice(2);
    if (action !== "watch" || !directory) throw new Error("Use watch with an owned task directory");
    const stop = watchFrontendIdentity(root, join(directory, "frontend-workspace"));
    for (const signal of ["SIGINT", "SIGTERM"]) process.on(signal, () => { stop(); process.exit(0); });
}
