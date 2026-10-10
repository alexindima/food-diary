import { randomUUID } from "node:crypto";
import { spawnSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { featurePlan, repositoryRoot } from "./generate-feature.mjs";

const directory = join(
    repositoryRoot,
    ".artifacts/ai-development-foundation/generator-builds",
    randomUUID(),
);
mkdirSync(directory, { recursive: true });
const owner = "Modules/Hydration/Application/";
for (const kind of ["query", "command"]) {
    const plan = featurePlan({
        target: "backend",
        module: "Hydration",
        name: `Generated${kind === "query" ? "Read" : "Write"}Probe`,
        kind,
        response: "int",
    });
    for (const file of plan.files) {
        const target = join(directory, file.path.slice(owner.length));
        mkdirSync(dirname(target), { recursive: true });
        writeFileSync(target, file.content);
    }
}
const references = [
    "Shared/FoodDiary.Application.Contracts/FoodDiary.Application.Contracts.csproj",
    "Shared/FoodDiary.Results/FoodDiary.Results.csproj",
    "Shared/FoodDiary.Mediator/FoodDiary.Mediator.csproj",
];
const project = `<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><RootNamespace>FoodDiary.Modules.Hydration.Application</RootNamespace><NuGetLockFilePath>$(MSBuildProjectDirectory)/packages.lock.json</NuGetLockFilePath></PropertyGroup><ItemGroup>${references.map((path) => `<ProjectReference Include="${join(repositoryRoot, path)}" />`).join("")}<PackageReference Include="FluentValidation" /></ItemGroup></Project>`;
const projectPath = join(directory, "FoodDiary.GeneratedProof.csproj");
writeFileSync(projectPath, project);
const result = spawnSync(
    "dotnet",
    ["build", projectPath, "--artifacts-path", join(directory, "build")],
    {
        cwd: repositoryRoot,
        encoding: "utf8",
        windowsHide: true,
        timeout: 180_000,
        maxBuffer: 8 * 1024 * 1024,
    },
);
writeFileSync(
    join(directory, "build.log"),
    `${result.stdout ?? ""}\n${result.stderr ?? ""}`,
);
console.log(
    JSON.stringify({
        passed: result.status === 0,
        generatedRequests: 2,
        directory,
        logPath: join(directory, "build.log"),
    }),
);
if (result.error || result.status !== 0) process.exitCode = 1;
