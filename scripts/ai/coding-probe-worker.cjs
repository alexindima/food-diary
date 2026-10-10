// This trusted grader runs inside a network-disabled, read-only container.
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const { createRequire } = require("node:module");
const ts = require("/dependencies/node_modules/typescript");
const dependencies = createRequire("/dependencies/package.json");
const cache = new Map();
const root = "/candidate/FoodDiary.Web.Client/src";

function load(file) {
    const absolute = path.resolve(file);
    if (cache.has(absolute)) return cache.get(absolute).exports;
    const code = ts.transpileModule(fs.readFileSync(absolute, "utf8"), {
        compilerOptions: {
            target: ts.ScriptTarget.ES2022,
            module: ts.ModuleKind.CommonJS,
        },
    }).outputText;
    const module = { exports: {} };
    cache.set(absolute, module);
    vm.runInNewContext(
        code,
        {
            module,
            exports: module.exports,
            require: (name) =>
                name.startsWith(".")
                    ? load(path.resolve(path.dirname(absolute), `${name}.ts`))
                    : dependencies(name),
        },
        { filename: absolute, timeout: 5000 },
    );
    return module.exports;
}

function same(actual, expected) {
    if (actual !== expected)
        throw new Error(
            "A semantic constructor or conversion changed its native value",
        );
}

try {
    const id = process.argv[2];
    const model = (name) =>
        path.join(root, `app/shared/models/semantics/${name}.ts`);
    if (id === "calendar-meaning") {
        const dates = load(model("date-value"));
        same(
            dates.utcInstant("2026-10-08T23:59:59.1234567Z"),
            "2026-10-08T23:59:59.1234567Z",
        );
        same(dates.calendarDate("2026-10-08"), "2026-10-08");
        same(dates.optionalCalendarDate(null), null);
        same(dates.optionalCalendarDate(undefined), undefined);
    } else if (id === "usda-link") {
        const food = load(model("usda-food-id"));
        for (const value of [17000, 0, 12.125])
            same(food.usdaFoodId(value), value);
    } else if (id === "recipe-quantity") {
        const quantities = load(model("recipe-quantity"));
        same(quantities.recipeDisplayGramsFromInput(12.125), 12.125);
        same(quantities.recipeServingMass(50), 50);
        const display = load(
            path.join(
                root,
                "app/features/meals/lib/recipe-serving/recipe-display-amount.ts",
            ),
        );
        same(
            display.recipeDisplayToServings({
                unit: "grams",
                value: 100,
                gramsPerServing: 50,
            }),
            2,
        );
    } else if (id === "isolation-proof") {
        const proof = load(model("isolation-proof"));
        same(proof.candidateReadDenied(), true);
        same(proof.candidateWriteDenied(), true);
        same(proof.networkDenied(), true);
    } else {
        const identity = load(model("entity-id"));
        const value = "00000000-0000-4000-8000-000000000001";
        same(identity.entityId(value), value);
    }
    process.stdout.write(JSON.stringify({ passed: true, id }));
} catch (error) {
    process.stdout.write(
        JSON.stringify({
            passed: false,
            error: String(error.message).slice(0, 1000),
        }),
    );
    process.exitCode = 1;
}
