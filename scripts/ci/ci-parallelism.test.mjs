import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync, readdirSync, mkdirSync, writeFileSync } from "node:fs";
import { createRequire } from "node:module";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { join } from "node:path";
import { randomUUID } from "node:crypto";

const root = fileURLToPath(new URL("../../", import.meta.url));
const require = createRequire(
    new URL("../../FoodDiary.Web.Client/package.json", import.meta.url),
);
const { parse } = require("yaml");
const workflow = parse(
    readFileSync(join(root, ".github/workflows/ci-tests.yml"), "utf8"),
);
const { jobs } = workflow;

test("hosted Ubuntu runners use an explicit image and artifact uploads share the Node 24 action pin", () => {
    const expectedUpload = jobs["llm-wiki-focused"].steps.find(step => step.uses?.startsWith("actions/upload-artifact@")).uses;
    for (const file of readdirSync(join(root, ".github/workflows")).filter(file => file.endsWith(".yml"))) {
        const current = parse(readFileSync(join(root, ".github/workflows", file), "utf8"));
        for (const [name, job] of Object.entries(current.jobs)) {
            const runner = job["runs-on"];
            if (typeof runner === "string" && runner.startsWith("ubuntu-")) {
                assert.match(runner, /^ubuntu-\d+\.\d+$/u, `${file}/${name}: choose an OS version deliberately`);
            }
            for (const step of job.steps ?? []) {
                if (step.uses?.startsWith("actions/upload-artifact@")) assert.equal(step.uses, expectedUpload, `${file}/${name}`);
            }
        }
    }
    assert.equal(expectedUpload, "actions/upload-artifact@cf430e030ddbb5b0abf93d22962f4752f3646cd9");
});

test("Wiki failure diagnostics exist before checks and retain the failing source-check transcript", () => {
    const steps = jobs["llm-wiki-focused"].steps;
    const initialize = steps.findIndex(step => step.name === "Initialize Wiki diagnostics");
    const verify = steps.findIndex(step => step.name === "Verify Wiki checks and focused partition");
    assert.ok(initialize >= 0 && initialize < verify);
    assert.match(steps[initialize].run, /Set-Content \.artifacts\/llm-wiki\/ci\/run\.log/u);
    assert.match(steps[verify].run, /Start-Transcript -Path \.artifacts\/llm-wiki\/ci\/checks\.log/u);
    assert.match(steps[verify].run, /finally\s*\{\s*Stop-Transcript/u);
    const upload = steps.find(step => step.name === "Upload Wiki failure diagnostics");
    assert.ok(upload.with.path.includes(".artifacts/llm-wiki/**/*.log"));
});

test("all original frontend checks run exactly once across four independent lanes", () => {
    const lanes = jobs["frontend-lanes"];
    assert.equal(lanes.needs, undefined);
    assert.equal(lanes.strategy["fail-fast"], false);
    assert.equal(lanes.strategy["max-parallel"], 4);
    const groups = lanes.strategy.matrix.group;
    assert.deepEqual(groups, [
        "quality-build",
        "app-tests",
        "libraries",
        "smoke",
    ]);
    const required = {
        "quality-build": [
            "Check i18n consistency",
            "Lint frontend",
            "Lint frontend dependency graph",
            "Lint styles",
            "Check formatting",
            "Check component logic boundaries",
            "Audit design token references",
            "Check frontend state ownership",
            "Build frontend",
            "Check prerendered SEO HTML",
            "Build admin frontend",
            "Build fd-tour",
            "Build fd-webmcp",
            "Build Storybook",
        ],
        "app-tests": ["Run frontend app unit tests"],
        libraries: [
            "Verify measurement and dashboard time zones",
            "Run fd-tour unit tests",
            "Run fd-webmcp unit tests",
            "Run admin frontend unit tests",
            "Run fd-ui-kit unit tests",
        ],
        smoke: [
            "Cache Playwright browsers",
            "Install Playwright Chromium",
            "Run client smoke E2E",
            "Run admin smoke E2E",
        ],
    };
    for (const [group, names] of Object.entries(required)) {
        for (const name of names) {
            const steps = lanes.steps.filter((step) => step.name === name);
            assert.equal(steps.length, 1, name);
            assert.equal(steps[0].if, "matrix.group == '" + group + "'", name);
            if (steps[0].run)
                assert.doesNotMatch(
                    steps[0].run,
                    /continue-on-error|--passWithNoTests/u,
                );
        }
    }
    const commands = lanes.steps.map((step) => step.run ?? "").join("\n");
    for (const project of ["app", "admin", "ui-kit", "tour", "webmcp"]) {
        assert.ok(
            commands.includes("npm run test:coverage:" + project),
            "Missing coverage: " + project,
        );
    }
    for (const command of [
        "check:depcruise-baseline",
        "check:seo-prerender",
        "build:storybook",
        "test:ci:measurement-timezones",
    ]) {
        assert.ok(
            commands.includes("npm run " + command),
            "Missing check: " + command,
        );
    }
    assert.equal(lanes.env.FRONTEND_GROUP, "${{ matrix.group }}");
    assert.match(
        lanes.steps.find(
            (step) => step.name === "Initialize frontend diagnostics",
        ).run,
        /GITHUB_ENV/u,
    );
    const failure = lanes.steps.find(
        (step) => step.name === "Upload frontend failure logs",
    );
    assert.equal(
        failure.with.name,
        "ci-failure-logs-frontend-${{ matrix.group }}",
    );
    const coverage = lanes.steps.find(
        (step) => step.name === "Upload frontend coverage reports",
    );
    assert.equal(
        coverage.with.name,
        "frontend-checks-coverage-${{ matrix.group }}",
    );
    assert.equal(
        coverage.if,
        "always() && (matrix.group == 'app-tests' || matrix.group == 'libraries')",
    );
});

test("frontend aggregate rejects failure, cancellation, skips and missing execution, preserving each lane diagnostic", () => {
    const gate = jobs["frontend-checks"];
    assert.equal(gate.name, "Frontend checks, tests, and builds");
    assert.equal(gate.if, "always()");
    assert.equal(gate.needs, "frontend-lanes");
    const download = gate.steps.find(
        (step) => step.name === "Download frontend failure diagnostics",
    );
    assert.equal(download.with.pattern, "ci-failure-logs-frontend-*");
    assert.equal(download.with["merge-multiple"], true);
    const step = gate.steps.find(
        (step) => step.name === "Require all frontend lanes",
    );
    assert.equal(
        step.env.FRONTEND_RESULT,
        "${{ needs.frontend-lanes.result }}",
    );
    for (const result of ["success", "failure", "cancelled", "skipped", ""]) {
        const temp = join(root, ".artifacts/ci-plan-tests", randomUUID());
        const logs = join(temp, "frontend-checks-logs");
        for (const lane of ["app-tests", "quality-build"]) {
            const directory = join(logs, lane);
            mkdirSync(directory, { recursive: true });
            writeFileSync(
                join(directory, "failure-meta.env"),
                "FAILED_AREA=frontend\nFAILED_TARGET=" +
                    lane +
                    "\nFAILED_LOG_FILE_NAME=npm-ci.log\n",
            );
            writeFileSync(join(directory, "npm-ci.log"), lane);
        }
        const execution = spawnSync(
            "pwsh",
            ["-NoProfile", "-NonInteractive", "-Command", step.run],
            {
                env: {
                    ...process.env,
                    FRONTEND_RESULT: result,
                    RUNNER_TEMP: temp,
                },
                encoding: "utf8",
            },
        );
        assert.equal(execution.error, undefined);
        assert.equal(
            execution.status === 0,
            result === "success",
            execution.stdout + execution.stderr,
        );
        if (result !== "success") {
            const metadata = readFileSync(
                join(logs, "failure-meta.env"),
                "utf8",
            );
            assert.match(
                metadata,
                /FAILED_LOG_FILE_NAME=app-tests\/npm-ci.log/u,
            );
            assert.equal(
                readFileSync(join(logs, "app-tests/npm-ci.log"), "utf8"),
                "app-tests",
            );
            assert.equal(
                readFileSync(join(logs, "quality-build/npm-ci.log"), "utf8"),
                "quality-build",
            );
        }
    }
    assert.equal(
        gate.steps.find(
            (step) => step.name === "Upload CI frontend failure logs",
        ).with.name,
        "ci-failure-logs-frontend",
    );
    assert.ok(jobs["notify-ci-failure"].needs.includes("frontend-checks"));
});

test("backend starts every group independently and still requires all groups and formatting", () => {
    assert.equal(jobs["backend-slow"].needs, undefined);
    assert.equal(jobs["backend-slow"].strategy["fail-fast"], false);
    assert.equal(jobs["backend-slow"].strategy["max-parallel"], 4);
    assert.deepEqual(jobs["backend-slow"].strategy.matrix.group, [
        "integration-1",
        "integration-2",
        "integration-3",
        "mcp",
    ]);
    assert.deepEqual(jobs["dotnet-tests"].needs, [
        "backend-format",
        "backend-fast",
        "backend-slow",
    ]);
    assert.equal(jobs["dotnet-tests"].if, "always()");
});

test("Wiki requires common checks, the entire isolated smoke matrix and every applicable Full audit shard", () => {
    const smoke = jobs["llm-wiki-smoke"];
    assert.equal(smoke.needs, undefined);
    assert.equal(jobs["llm-wiki-focused"].needs, undefined);
    assert.equal(smoke.strategy["fail-fast"], false);
    const manifest = JSON.parse(
        readFileSync(join(root, "scripts/ci/wiki-smoke-shards.json"), "utf8"),
    );
    assert.deepEqual(
        smoke.strategy.matrix.shard,
        manifest.shards.map((shard) => shard.name),
    );
    assert.ok(smoke.steps.some((step) => step.with?.["fetch-depth"] === 0));
    const run = smoke.steps.find(
        (step) => step.name === "Run complete focused partition",
    ).run;
    assert.match(run, /Get-WikiSmokeShard\.ps1/u);
    assert.ok(
        run.includes("-RequestedGroup $groups -MaxConcurrency 2 -NoCache"),
    );
    assert.deepEqual(jobs["llm-wiki"].needs, [
        "llm-wiki-focused",
        "llm-wiki-smoke",
        "llm-wiki-audit",
    ]);
    assert.equal(jobs["llm-wiki"].if, "always()");
    assert.equal(
        jobs["llm-wiki"].steps.find(
            (step) => step.name === "Require all applicable Wiki checks",
        ).env.WIKI_SMOKE_RESULT,
        "${{ needs.llm-wiki-smoke.result }}",
    );
    const checks = readFileSync(
        join(root, "scripts/ci/Invoke-WikiCiChecks.ps1"),
        "utf8",
    );
    for (const name of [
        "Get-LlmWikiWorkspacePolicy",
        "Test-LlmWikiPortable",
        "Test-LlmWikiSessionResolution",
        "Test-LlmWikiProcess",
        "Invoke-LlmWikiIndexPipeline",
        "Manage-LlmWikiFailures",
        "Test-LlmWikiChangePolicy",
        "Get-LlmWikiImpact",
    ]) {
        assert.ok(
            checks.includes(name + ".ps1"),
            "Lost common Wiki gate: " + name,
        );
    }
    assert.doesNotMatch(checks, /Write-LlmWikiIndexVerificationReceipt/u);
    assert.match(
        jobs["llm-wiki-focused"].steps.find(
            (step) => step.name === "Verify Wiki checks and focused partition",
        ).run,
        /node --test scripts\/ci\/ci-parallelism\.test\.mjs/u,
    );
});
