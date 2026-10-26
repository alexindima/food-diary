import { randomUUID, createHash } from "node:crypto";
import { spawnSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { cpus, totalmem, platform } from "node:os";
import { repositoryRoot } from "./generate-feature.mjs";
import { ownedApi, ownedRuntime } from "./owned-runtime-client.mjs";
import { acquireRuntimeCheck } from "./runtime-check-lease.mjs";
import { pinnedK6 } from "./pinned-k6.mjs";

export function loadPlan({
    products = 1500,
    recipes = 100,
    meals = 300,
    levels = [1, 5, 10],
    repetitions = 2,
    holdSeconds = 15,
} = {}) {
    if (
        ![products, recipes, meals].every(
            (value) => Number.isInteger(value) && value > 0 && value <= 5000,
        ) ||
        !levels.every(
            (value) => Number.isInteger(value) && value >= 1 && value <= 50,
        ) ||
        !Number.isInteger(repetitions) ||
        repetitions < 1 ||
        repetitions > 5 ||
        !Number.isInteger(holdSeconds) ||
        holdSeconds < 5 ||
        holdSeconds > 600
    )
        throw new Error("Invalid bounded load plan");
    return { products, recipes, meals, levels, repetitions, holdSeconds };
}

async function batch(count, action) {
    let next = 0;
    await Promise.all(
        Array.from({ length: 4 }, async () => {
            while (next < count) {
                const index = next++;
                await action(index);
            }
        }),
    );
}

export async function loadBaseline(name, root = repositoryRoot, options) {
    const release = acquireRuntimeCheck(ownedRuntime(name, root), "load");
    try {
        return await executeLoad(name, root, options);
    } finally {
        release();
    }
}

async function executeLoad(name, root, options) {
    const plan = loadPlan(options);
    const client = await ownedApi(name, root);
    const executable = await pinnedK6(root);
    for (const path of [
        "/api/v1/dashboard?date=2026-09-15T00%3A00%3A00.000Z&timeZoneId=UTC&timeZoneOffsetMinutes=0&page=1&limit=25&locale=en&trendDays=7",
        "/api/v1/products?page=1&limit=25&includePublic=false",
        "/api/v1/recipes?page=1&limit=25&includePublic=false",
        "/api/v1/meals?page=1&limit=25",
    ])
        await client.request("GET", path);
    const directory = join(root, ".artifacts/reliability", name);
    mkdirSync(directory, { recursive: true });
    const manifest = {
        version: 1,
        id: randomUUID(),
        products: [],
        recipes: [],
        meals: [],
    };
    const manifestPath = join(directory, "load-fixtures.json");
    const save = () =>
        writeFileSync(manifestPath, JSON.stringify(manifest), { mode: 0o600 });
    const runs = [];
    const report = {
        schemaVersion: 1,
        scope: "isolated workstation regression baseline; not production capacity",
        runtime: name,
        sourceFingerprint: client.state.sourceFingerprint,
        plan,
        environment: {
            platform: platform(),
            cpu: cpus()[0]?.model,
            logicalCpus: cpus().length,
            memoryGiB: totalmem() / 1024 ** 3,
        },
        fixtureId: manifest.id,
        runs,
    };
    try {
        console.log(
            `Load: seeding ${plan.products} products, ${plan.recipes} recipes and ${plan.meals} meals through owner APIs`,
        );
        save();
        await batch(plan.products, async (index) => {
            const result = await client.request(
                "POST",
                "/api/v1/products",
                {
                    name: `fd-load-${manifest.id}-${index}`,
                    brand: "Owned load fixture",
                    productType: "Grain",
                    baseUnit: "G",
                    baseAmount: 100,
                    defaultPortionAmount: 100,
                    caloriesPerBase: 120,
                    proteinsPerBase: 10,
                    fatsPerBase: 5,
                    carbsPerBase: 20,
                    fiberPerBase: 3,
                    alcoholPerBase: 0,
                    visibility: "Private",
                },
                [201],
            );
            if (typeof result.id !== "string")
                throw new Error("Product fixture returned no identity");
            manifest.products.push(result.id);
            save();
        });
        await batch(plan.recipes, async (index) => {
            const result = await client.request(
                "POST",
                "/api/v1/recipes",
                {
                    name: `fd-load-${manifest.id}-recipe-${index}`,
                    servings: 2,
                    visibility: "Private",
                    calculateNutritionAutomatically: true,
                    steps: [
                        {
                            description: "Owned fixture",
                            ingredients: [
                                {
                                    productId:
                                        manifest.products[
                                            index % manifest.products.length
                                        ],
                                    amount: 250,
                                },
                            ],
                        },
                    ],
                },
                [201],
            );
            if (typeof result.id !== "string")
                throw new Error("Recipe fixture returned no identity");
            manifest.recipes.push(result.id);
            save();
        });
        await batch(plan.meals, async (index) => {
            const result = await client.request(
                "POST",
                "/api/v1/meals",
                {
                    date: new Date(
                        Date.UTC(2026, 8, 1 + (index % 30), 12),
                    ).toISOString(),
                    mealType: "Lunch",
                    comment: `fd-load-${manifest.id}`,
                    isNutritionAutoCalculated: true,
                    items: [
                        {
                            productId:
                                manifest.products[
                                    index % manifest.products.length
                                ],
                            amount: 100,
                            origin: "Manual",
                        },
                    ],
                },
                [201],
            );
            if (typeof result.id !== "string")
                throw new Error("Meal fixture returned no identity");
            manifest.meals.push(result.id);
            save();
        });
        report.datasetSha256 = createHash("sha256")
            .update(JSON.stringify(manifest))
            .digest("hex");
        for (const vus of plan.levels)
            for (
                let repetition = 1;
                repetition <= plan.repetitions;
                repetition++
            ) {
                console.log(
                    `Load: ${vus} concurrent users, repetition ${repetition}`,
                );
                const output = join(directory, `k6-${vus}-${repetition}.json`);
                const result = spawnSync(
                    executable,
                    [
                        "run",
                        "--summary-export",
                        output,
                        join(root, "infra/load-testing/api-baseline.js"),
                    ],
                    {
                        cwd: root,
                        env: {
                            ...process.env,
                            BASE_URL: client.state.apiUrl,
                            BROWSER_COOKIE: client.cookie,
                            VUS: String(vus),
                            RAMP_DURATION: "2s",
                            HOLD_DURATION: `${plan.holdSeconds}s`,
                            RAMP_DOWN_DURATION: "2s",
                            GRACE_DURATION: "2s",
                            ITERATION_PAUSE_SECONDS: "0.2",
                            K6_NO_USAGE_REPORT: "true",
                        },
                        encoding: "utf8",
                        windowsHide: true,
                        timeout: (plan.holdSeconds + 90) * 1000,
                        maxBuffer: 4 * 1024 * 1024,
                    },
                );
                if (result.error || !readFileSync(output, "utf8"))
                    throw new Error(
                        "Load process failed before producing its summary",
                    );
                const summary = JSON.parse(readFileSync(output, "utf8"));
                runs.push({
                    vus,
                    repetition,
                    passed: result.status === 0,
                    metrics: summary.metrics,
                });
            }
        report.status = runs.every((run) => run.passed) ? "passed" : "failed";
    } catch (error) {
        report.status = "failed";
        report.error = error.message;
        throw error;
    } finally {
        console.log(
            "Load: removing only exact fixture identities created by this run",
        );
        const failures = [];
        for (const [collection, path] of [
            [manifest.meals, "meals"],
            [manifest.recipes, "recipes"],
            [manifest.products, "products"],
        ])
            for (const id of collection) {
                try {
                    await client.request(
                        "DELETE",
                        `/api/v1/${path}/${id}`,
                        undefined,
                        [204, 404],
                    );
                } catch {
                    failures.push({ kind: path, id });
                }
            }
        report.cleanup = {
            removed:
                manifest.meals.length +
                manifest.recipes.length +
                manifest.products.length -
                failures.length,
            failures,
        };
        if (failures.length) report.status = "failed";
        writeFileSync(
            join(directory, "load-baseline.json"),
            `${JSON.stringify(report, null, 2)}\n`,
        );
    }
    if (report.status !== "passed")
        throw new Error(
            "Load thresholds or fixture cleanup failed; inspect owned evidence",
        );
    return report;
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
)
    console.log(JSON.stringify(await loadBaseline(process.argv[2])));
