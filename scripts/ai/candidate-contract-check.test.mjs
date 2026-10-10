import test from "node:test";
import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { mkdirSync, writeFileSync, copyFileSync, realpathSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";

test("candidate checker resolves real package exports and catches an optional-field type error", () => {
    const root = join(
        repositoryRoot,
        `.artifacts/candidate-check-tests/${randomUUID()}`,
    );
    mkdirSync(join(root, "FoodDiary.Web.Client/src"), { recursive: true });
    writeFileSync(
        join(root, "FoodDiary.Web.Client/tsconfig.json"),
        JSON.stringify({
            compilerOptions: {
                target: "ES2022",
                module: "ES2022",
                strict: true,
                moduleResolution: "bundler",
            },
        }),
    );
    copyFileSync(
        join(repositoryRoot, "scripts/ai/candidate-contract-check.cjs"),
        join(root, "verify-contract.cjs"),
    );
    writeFileSync(
        join(root, "contract-check.json"),
        JSON.stringify({
            dependenciesPath: realpathSync(
                join(repositoryRoot, "FoodDiary.Web.Client/node_modules"),
            ),
        }),
    );
    writeFileSync(
        join(root, "acceptance.ts"),
        "import type { HttpHeaders } from '@angular/common/http';\ndeclare const headers: HttpHeaders;\ndeclare const optional: number | undefined;\nconst value: number = optional;\nvoid headers; void value;\n",
    );
    const failed = spawnSync(
        process.execPath,
        [join(root, "verify-contract.cjs")],
        { encoding: "utf8", windowsHide: true },
    );
    assert.equal(failed.status, 1);
    assert.match(failed.stdout, /TS2322/u);
    assert.equal(failed.stdout.includes("TS2307"), false);
    writeFileSync(
        join(root, "acceptance.ts"),
        "import { HttpHeaders } from '@angular/common/http';\nconst headers = new HttpHeaders();\nvoid headers;\n",
    );
    assert.equal(
        spawnSync(process.execPath, [join(root, "verify-contract.cjs")], {
            encoding: "utf8",
            windowsHide: true,
        }).status,
        0,
    );
});
