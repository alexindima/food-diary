import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join, resolve } from "node:path";
import test from "node:test";
import {
    applyFeaturePlan,
    containedPath,
    featurePlan,
    parseOptions,
    repositoryRoot,
} from "./generate-feature.mjs";

const ts = createRequire(
    join(repositoryRoot, "FoodDiary.Web.Client/package.json"),
)("typescript");

test("dry-run uses actual owner namespaces and adds no files or references", () => {
    const plan = featurePlan({
        target: "backend",
        module: "Hydration",
        name: `Inspect${randomUUID().replaceAll("-", "")}`,
        kind: "query",
    });
    assert.equal(plan.readyForProduction, false);
    assert.equal(plan.files.length, 3);
    assert.ok(
        plan.files.every(
            (file) => !existsSync(join(repositoryRoot, file.path)),
        ),
    );
    assert.ok(
        plan.files.every((file) =>
            file.path.startsWith("Modules/Hydration/Application/Queries/"),
        ),
    );
});

test("rejects unknown owner, path traversal and shell-shaped names before mutation", () => {
    assert.throws(
        () =>
            featurePlan({
                target: "backend",
                module: "../Users",
                name: "Inspect",
            }),
        /known module/u,
    );
    assert.throws(
        () =>
            featurePlan({
                target: "backend",
                module: "AbsentModule",
                name: "Inspect",
            }),
        /already exist/u,
    );
    assert.throws(
        () =>
            featurePlan({
                target: "frontend",
                feature: "hydration",
                name: "Unsafe;Run",
                sdk: "hydration",
                operation: "getHydrationDaily",
            }),
        /owning feature/u,
    );
    assert.throws(
        () => containedPath(repositoryRoot, "../outside.txt"),
        /inside/u,
    );
    if (process.platform === "win32") {
        const foreignDrive = repositoryRoot.toLowerCase().startsWith("c:")
            ? "D:"
            : "C:";
        assert.throws(
            () => containedPath(repositoryRoot, `${foreignDrive}/outside.txt`),
            /inside/u,
        );
    }
    assert.throws(() => parseOptions(["backend", "--name"]), /Missing/u);
    assert.throws(
        () => parseOptions(["backend", "--apply", "--dry-run"]),
        /Choose/u,
    );
});

test("application refuses every collision before writing any generated file", () => {
    const directory = containedPath(
        repositoryRoot,
        `.artifacts/ai-development-foundation/generator-safety/${randomUUID()}`,
    );
    mkdirSync(directory, { recursive: true });
    writeFileSync(join(directory, "keep.txt"), "preserved");
    const plan = {
        files: [
            { path: "new.txt", content: "new" },
            { path: "keep.txt", content: "replacement" },
        ],
    };
    assert.throws(() => applyFeaturePlan(plan, directory), /overwrite/u);
    assert.equal(
        readFileSync(join(directory, "keep.txt"), "utf8"),
        "preserved",
    );
    assert.equal(existsSync(join(directory, "new.txt")), false);
});

test("generated frontend adapter and facade compile against the actual SDK", () => {
    const plan = featurePlan({
        target: "frontend",
        feature: "hydration",
        name: "GeneratorProbe",
        sdk: "hydration",
        operation: "getHydrationDaily",
    });
    const client = join(repositoryRoot, "FoodDiary.Web.Client");
    const config = ts.readConfigFile(
        join(client, "tsconfig.json"),
        ts.sys.readFile,
    );
    const options = {
        ...ts.parseJsonConfigFileContent(config.config, ts.sys, client).options,
        noEmit: true,
        skipLibCheck: true,
        strict: true,
        experimentalDecorators: true,
    };
    const virtual = new Map(
        plan.files.map((file) => [
            resolve(repositoryRoot, file.path).replaceAll("\\", "/"),
            file.content,
        ]),
    );
    const host = ts.createCompilerHost(options);
    const read = host.readFile.bind(host);
    const exists = host.fileExists.bind(host);
    host.readFile = (path) =>
        virtual.get(resolve(path).replaceAll("\\", "/")) ?? read(path);
    host.fileExists = (path) =>
        virtual.has(resolve(path).replaceAll("\\", "/")) || exists(path);
    const program = ts.createProgram(
        [...virtual.keys(), join(client, "src/types/barcode-detector.d.ts")],
        options,
        host,
    );
    const diagnostics = ts.getPreEmitDiagnostics(program);
    assert.equal(
        diagnostics.length,
        0,
        ts.formatDiagnostics(diagnostics, {
            getCurrentDirectory: () => client,
            getCanonicalFileName: (path) => path,
            getNewLine: () => "\n",
        }),
    );
});

test("missing SDK operation is rejected rather than generated as a manual HTTP client", () => {
    assert.throws(
        () =>
            featurePlan({
                target: "frontend",
                feature: "hydration",
                name: "GeneratorProbe",
                sdk: "hydration",
                operation: "inventedEndpoint",
            }),
        /absent/u,
    );
});
