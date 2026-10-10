import { createHash, randomUUID } from "node:crypto";
import { spawnSync } from "node:child_process";
import {
    existsSync,
    mkdirSync,
    readFileSync,
    realpathSync,
    readdirSync,
    writeFileSync,
} from "node:fs";
import { createRequire } from "node:module";
import { dirname, isAbsolute, join, relative, resolve, sep } from "node:path";
import { pathToFileURL } from "node:url";
import { isolatedRuntimeProbes } from "./coding-probe-isolation.mjs";
import { containedPath, repositoryRoot } from "./generate-feature.mjs";

const requireClient = createRequire(
    join(repositoryRoot, "FoodDiary.Web.Client/package.json"),
);
const ts = requireClient("typescript");
const corpusPath = join(
    repositoryRoot,
    "Tooling/ai-development/coding-evals/cases.json",
);
const corpus = JSON.parse(readFileSync(corpusPath, "utf8"));
const hash = (value) => createHash("sha256").update(value).digest("hex");
const save = (path, value) =>
    writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`);

function git(args) {
    const result = spawnSync("git", args, {
        cwd: repositoryRoot,
        encoding: "utf8",
        windowsHide: true,
        maxBuffer: 8 * 1024 * 1024,
    });
    if (result.status !== 0)
        throw new Error(`Git failed: ${result.stderr.trim()}`);
    return result.stdout.trim();
}

export function codingCase(id) {
    const entry = corpus.cases.find((item) => item.id === id);
    if (!entry || !/^[a-f0-9]{40}$/u.test(entry.baseline))
        throw new Error(
            "Choose a maintained coding case with an immutable baseline",
        );
    if (git(["rev-parse", `${entry.baseline}^{commit}`]) !== entry.baseline)
        throw new Error("Coding baseline is unavailable");
    return entry;
}

function fingerprints(root) {
    const files = {};
    const walk = (directory) => {
        for (const entry of readdirSync(directory, { withFileTypes: true })) {
            const path = join(directory, entry.name);
            if (entry.isSymbolicLink())
                throw new Error(
                    "Candidate snapshots must not contain symbolic links",
                );
            if (entry.isDirectory()) walk(path);
            else {
                const content = readFileSync(path);
                files[relative(root, path).replaceAll("\\", "/")] = createHash(
                    "sha1",
                )
                    .update(`blob ${content.length}\0`)
                    .update(content)
                    .digest("hex");
            }
        }
    };
    walk(join(root, "FoodDiary.Web.Client"));
    return files;
}

function baselineFiles(revision) {
    const rows = git([
        "ls-tree",
        "-r",
        revision,
        "--",
        "FoodDiary.Web.Client/src",
        "FoodDiary.Web.Client/projects",
        "FoodDiary.Web.Client/tsconfig.json",
    ]).split("\n");
    return Object.fromEntries(
        rows.filter(Boolean).map((row) => {
            const [metadata, path] = row.split("\t");
            const [mode, type, object] = metadata.split(" ");
            if (type !== "blob" || mode === "120000")
                throw new Error(
                    "Coding baseline must contain ordinary files only",
                );
            return [path, object];
        }),
    );
}

export function prepareCase(id) {
    const entry = codingCase(id);
    const directory = containedPath(
        repositoryRoot,
        `.artifacts/coding-evals/${id}-${randomUUID()}`,
    );
    const candidate = join(directory, "candidate");
    mkdirSync(candidate, { recursive: true });
    const archive = join(directory, "baseline.tar");
    git([
        "archive",
        "--format=tar",
        `--output=${archive}`,
        entry.baseline,
        "FoodDiary.Web.Client/src",
        "FoodDiary.Web.Client/projects",
        "FoodDiary.Web.Client/tsconfig.json",
    ]);
    const extraction = spawnSync("tar", ["-xf", archive, "-C", candidate], {
        windowsHide: true,
        encoding: "utf8",
    });
    if (extraction.status !== 0)
        throw new Error("Cannot extract the owned coding snapshot");
    const grader = join(dirname(corpusPath), "graders", entry.grader);
    const record = {
        schemaVersion: 1,
        id,
        baseline: entry.baseline,
        candidate,
        task: entry.task,
        allowedPrefixes: entry.allowedPrefixes,
        corpusHash: hash(readFileSync(corpusPath)),
        graderHash: hash(readFileSync(grader)),
        dependencyLockHash: hash(
            readFileSync(
                join(repositoryRoot, "FoodDiary.Web.Client/package-lock.json"),
            ),
        ),
        preparedAtUtc: new Date().toISOString(),
        baselineFiles: baselineFiles(entry.baseline),
    };
    save(join(directory, "prepared.json"), record);
    writeFileSync(
        join(directory, "task.txt"),
        `${entry.task}\n\nEdit only: ${entry.allowedPrefixes.join(", ")}\nThe grader is maintained outside this candidate and must not be edited.\n`,
    );
    return record;
}

export function gradeCase(id, candidate, reference = false) {
    const entry = codingCase(id);
    candidate = resolve(candidate);
    const grader = join(dirname(corpusPath), "graders", entry.grader);
    const started = performance.now();
    const scopeViolations = [];
    let changedPaths = [];
    if (!reference) {
        const owned = containedPath(
            repositoryRoot,
            relative(repositoryRoot, candidate),
        );
        if (
            owned !== candidate ||
            !candidate.includes(`${join(".artifacts", "coding-evals")}`)
        )
            throw new Error("Grade only a prepared owned coding candidate");
        const record = JSON.parse(
            readFileSync(join(dirname(candidate), "prepared.json"), "utf8"),
        );
        if (
            record.id !== id ||
            record.baseline !== entry.baseline ||
            record.corpusHash !== hash(readFileSync(corpusPath)) ||
            record.graderHash !== hash(readFileSync(grader)) ||
            record.dependencyLockHash !==
                hash(
                    readFileSync(
                        join(
                            repositoryRoot,
                            "FoodDiary.Web.Client/package-lock.json",
                        ),
                    ),
                )
        )
            throw new Error(
                "Grader, baseline, corpus or dependencies changed; prepare a fresh trial",
            );
        const baseline = baselineFiles(entry.baseline);
        if (
            hash(JSON.stringify(record.baselineFiles)) !==
            hash(JSON.stringify(baseline))
        )
            throw new Error("Prepared baseline was modified");
        const current = fingerprints(candidate);
        changedPaths = [
            ...new Set([...Object.keys(baseline), ...Object.keys(current)]),
        ].filter((path) => baseline[path] !== current[path]);
        scopeViolations.push(
            ...changedPaths.filter(
                (path) =>
                    !entry.allowedPrefixes.some((prefix) =>
                        path.startsWith(prefix),
                    ),
            ),
        );
    }
    const client = join(candidate, "FoodDiary.Web.Client");
    const config = ts.readConfigFile(
        join(client, "tsconfig.json"),
        ts.sys.readFile,
    );
    if (config.error)
        throw new Error("Candidate has no valid TypeScript configuration");
    const base = ts.parseJsonConfigFileContent(
        config.config,
        ts.sys,
        client,
    ).options;
    const options = {
        ...base,
        strict: true,
        noEmit: true,
        skipLibCheck: true,
        experimentalDecorators: true,
        moduleResolution: ts.ModuleResolutionKind.Bundler,
        paths: {
            ...base.paths,
            "@candidate/*": [join(client, "src/*")],
        },
    };
    const host = ts.createCompilerHost(options);
    host.resolveModuleNames = (names, containingFile) =>
        names.map((name) => {
            const external =
                !name.startsWith(".") &&
                !name.startsWith("@candidate/") &&
                name !== "fd-tour" &&
                !name.startsWith("fd-ui-kit");
            const importer = external
                ? join(
                      repositoryRoot,
                      "FoodDiary.Web.Client/__coding_eval__.ts",
                  )
                : containingFile;
            return ts.resolveModuleName(name, importer, options, host)
                .resolvedModule;
        });
    const program = ts.createProgram(
        [grader, join(client, "src/types/barcode-detector.d.ts")],
        options,
        host,
    );
    const diagnostics = ts.getPreEmitDiagnostics(program);
    if (!reference) {
        const dependencyRoot = realpathSync(
            join(repositoryRoot, "FoodDiary.Web.Client/node_modules"),
        );
        const contained = (root, path) => {
            const local = relative(root, path);
            return (
                !isAbsolute(local) &&
                local !== ".." &&
                !local.startsWith(`..${sep}`)
            );
        };
        for (const source of program.getSourceFiles()) {
            const path = resolve(source.fileName);
            const insideCandidate = contained(candidate, path);
            const insideDependencies = contained(dependencyRoot, path);
            if (!insideCandidate && !insideDependencies && path !== grader) {
                scopeViolations.push(
                    `Source import escapes candidate: ${path}`,
                );
            }
        }
    }
    let runtimeError = null;
    if (diagnostics.length === 0) {
        try {
            isolatedRuntimeProbes(id, candidate);
        } catch (error) {
            runtimeError = error.message;
        }
    }
    const result = {
        id,
        baseline: entry.baseline,
        candidate,
        kind: reference ? "reference-validation" : "coding-outcome",
        passed:
            diagnostics.length === 0 &&
            runtimeError === null &&
            scopeViolations.length === 0,
        diagnosticCount: diagnostics.length,
        diagnostics: ts.formatDiagnostics(diagnostics, {
            getCurrentDirectory: () => repositoryRoot,
            getCanonicalFileName: (path) => path,
            getNewLine: () => "\n",
        }),
        runtimeError,
        scopeViolations,
        changedPaths,
        durationMs: Math.round(performance.now() - started),
        graderHash: hash(readFileSync(grader)),
        modelTrial: false,
    };
    if (!reference) save(join(dirname(candidate), "grade.json"), result);
    return result;
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    try {
        const [action, id, candidate] = process.argv.slice(2);
        if (action === "list") console.log(JSON.stringify(corpus, null, 2));
        else if (action === "prepare")
            console.log(JSON.stringify(prepareCase(id), null, 2));
        else if (action === "grade") {
            const result = gradeCase(id, candidate);
            console.log(JSON.stringify(result, null, 2));
            if (!result.passed) process.exitCode = 1;
        } else if (action === "self-test") {
            const results = [];
            for (const entry of corpus.cases) {
                const prepared = prepareCase(entry.id);
                const baseline = gradeCase(entry.id, prepared.candidate);
                const reference = gradeCase(entry.id, repositoryRoot, true);
                results.push({
                    id: entry.id,
                    baselineFails: !baseline.passed,
                    referencePasses: reference.passed,
                    reference,
                });
            }
            console.log(
                JSON.stringify(
                    {
                        kind: "grader-validation",
                        modelTrialsExecuted: 0,
                        passed: results.every(
                            (result) =>
                                result.baselineFails && result.referencePasses,
                        ),
                        results,
                    },
                    null,
                    2,
                ),
            );
            if (
                results.some(
                    (result) =>
                        !result.baselineFails || !result.referencePasses,
                )
            )
                process.exitCode = 1;
        } else
            throw new Error(
                "Use coding-evals.mjs list|prepare <case>|grade <case> <prepared-candidate>|self-test",
            );
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
