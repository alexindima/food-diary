// Copied into an owned candidate. The external grader remains authoritative.
const fs = require("node:fs");
const path = require("node:path");
const config = JSON.parse(
    fs.readFileSync(path.join(__dirname, "contract-check.json"), "utf8"),
);
const ts = require(path.join(config.dependenciesPath, "typescript"));
const client = path.join(__dirname, "FoodDiary.Web.Client");
const parsed = ts.readConfigFile(
    path.join(client, "tsconfig.json"),
    ts.sys.readFile,
);
if (parsed.error)
    throw new Error("Candidate TypeScript configuration is unavailable");
const base = ts.parseJsonConfigFileContent(
    parsed.config,
    ts.sys,
    client,
).options;
const options = {
    ...base,
    strict: true,
    noEmit: true,
    skipLibCheck: true,
    moduleResolution: ts.ModuleResolutionKind.Bundler,
    paths: { ...base.paths, "@candidate/*": [path.join(client, "src/*")] },
};
const host = ts.createCompilerHost(options);
host.resolveModuleNames = (names, containingFile) =>
    names.map((name) => {
        const external =
            !name.startsWith(".") &&
            !name.startsWith("@candidate/") &&
            name !== "fd-tour" &&
            !name.startsWith("fd-ui-kit");
        return ts.resolveModuleName(
            name,
            external
                ? path.join(
                      path.dirname(config.dependenciesPath),
                      "__eval__.ts",
                  )
                : containingFile,
            options,
            host,
        ).resolvedModule;
    });
const roots = [path.join(__dirname, "acceptance.ts")];
const browserTypes = path.join(client, "src/types/barcode-detector.d.ts");
if (fs.existsSync(browserTypes)) roots.push(browserTypes);
const program = ts.createProgram(roots, options, host);
const diagnostics = ts.getPreEmitDiagnostics(program);
process.stdout.write(
    diagnostics.length
        ? ts.formatDiagnostics(diagnostics, {
              getCurrentDirectory: () => __dirname,
              getCanonicalFileName: (value) => value,
              getNewLine: () => "\n",
          })
        : "Public acceptance contract passed against real candidate and dependency types.\n",
);
process.exitCode = diagnostics.length ? 1 : 0;
