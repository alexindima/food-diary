import { spawn } from "node:child_process";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";

const child = spawn(
    "dotnet",
    [
        "stryker",
        "--output",
        join(repositoryRoot, ".artifacts/ai-verification/mutation"),
    ],
    {
        cwd: join(
            repositoryRoot,
            "Modules/Recipes/tests/FoodDiary.Modules.Recipes.Domain.Tests",
        ),
        windowsHide: true,
        stdio: "inherit",
        env: {
            ...process.env,
            ArtifactsPath: join(
                repositoryRoot,
                ".artifacts/ai-verification/mutation-build",
            ),
        },
    },
);
child.once("error", (error) => {
    console.error(error.message);
    process.exitCode = 1;
});
child.once("close", (code) => {
    process.exitCode = code ?? 1;
});
