import { mkdirSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { repositoryRoot } from "./generate-feature.mjs";

export function referenceCorpus() {
    const texts = [
        ["apple-100", "100 g apple", "Apple", ["apple", "яблоко"], 100, "g"],
        ["rice-fraction", "12.5 g rice", "Rice", ["rice", "рис"], 12.5, "g"],
        ["milk-ml", "200 ml milk", "Milk", ["milk", "молоко"], 200, "ml"],
        [
            "egg-pieces",
            "2 pcs eggs",
            "Egg",
            ["egg", "eggs", "яйцо", "яйца"],
            2,
            "pcs",
        ],
        [
            "apple-quarter",
            "0.25 pcs apple",
            "Apple",
            ["apple", "яблоко"],
            0.25,
            "pcs",
        ],
        ["rice-small", "0.125 g rice", "Rice", ["rice", "рис"], 0.125, "g"],
        ["apple-ru", "Яблоко 100 г", "Apple", ["apple", "яблоко"], 100, "g"],
        ["rice-comma", "Рис 12,5 г", "Rice", ["rice", "рис"], 12.5, "g"],
        ["milk-ru", "Молоко 200 мл", "Milk", ["milk", "молоко"], 200, "ml"],
        [
            "egg-ru",
            "2 шт яйца",
            "Egg",
            ["egg", "eggs", "яйцо", "яйца"],
            2,
            "pcs",
        ],
    ].map(([id, text, name, aliases, amount, unit]) => ({
        id,
        kind: "text",
        locale: id.endsWith("ru") || id.includes("comma") ? "ru" : "en",
        input: { text },
        expected: { items: [{ nameEn: name, aliases, amount, unit }] },
    }));
    texts.push({
        id: "two-foods-en",
        kind: "text",
        locale: "en",
        input: { text: "100 g apple and 50 g rice" },
        expected: {
            items: [
                { nameEn: "Apple", aliases: ["apple"], amount: 100, unit: "g" },
                { nameEn: "Rice", aliases: ["rice"], amount: 50, unit: "g" },
            ],
        },
    });
    texts.push({
        id: "two-foods-ru",
        kind: "text",
        locale: "ru",
        input: { text: "Яблоко 100 г и рис 50 г" },
        expected: {
            items: [
                {
                    nameEn: "Apple",
                    aliases: ["apple", "яблоко"],
                    amount: 100,
                    unit: "g",
                },
                {
                    nameEn: "Rice",
                    aliases: ["rice", "рис"],
                    amount: 50,
                    unit: "g",
                },
            ],
        },
    });
    const labels = [
        ["label-100g", "en", 100, "g", 400, 10, 5, 60, 3, 0],
        ["label-portion", "en", 25, "g", 100, 2.5, 1.25, 15, 0.75, 0],
        ["label-zero", "en", 100, "ml", 0, 0, 0, 0, 0, 0],
        ["label-fraction", "en", 12.5, "g", 52.125, 0.125, 1.25, 10.5, 0, 0],
        ["label-ru", "ru", 100, "g", 400, 10, 5, 60, 3, 0],
        ["label-ru-portion", "ru", 25, "g", 100, 2.5, 1.25, 15, 0.75, 0],
        ["label-unreadable", "en", 100, "g", null, 10, 5, 60, null, null],
        ["label-pieces", "en", 1, "pcs", 123, 4, 5, 6, null, 0],
    ].map(
        ([
            id,
            locale,
            baseAmount,
            baseUnit,
            calories,
            protein,
            fat,
            carbs,
            fiber,
            alcohol,
        ]) => ({
            id,
            kind: "label",
            locale,
            input: { image: `fixtures/${id}.svg` },
            expected: {
                name: "REFERENCE FOOD",
                brand: "SYNTHETIC",
                baseAmount,
                baseUnit,
                calories,
                protein,
                fat,
                carbs,
                fiber,
                alcohol,
            },
        }),
    );
    return {
        schemaVersion: 1,
        version: "recognition-reference-v1",
        provenance:
            "Authored synthetic OCR labels and explicit-quantity text; fictional label nutrition; no private user content",
        coverage: {
            text: texts.length,
            label: labels.length,
            realFoodPhoto: 0,
        },
        limitations: [
            "Does not establish food-photo recognition or photo-derived mass accuracy",
            "Grader self-tests are not provider quality measurements",
        ],
        cases: [...texts, ...labels],
    };
}

export function writeReferenceCorpus(root = repositoryRoot) {
    const directory = join(root, "Tooling/ai-development/recognition-evals");
    mkdirSync(join(directory, "fixtures"), { recursive: true });
    const corpus = referenceCorpus();
    for (const item of corpus.cases.filter((value) => value.kind === "label")) {
        const label = item.expected;
        const ru = item.locale === "ru";
        const display = (value) =>
            value === null
                ? "UNREADABLE / ?"
                : String(value).replace(".", ru ? "," : ".");
        const lines = [
            "REFERENCE FOOD",
            "SYNTHETIC — FICTIONAL TEST LABEL",
            `${ru ? "На" : "Per"} ${display(label.baseAmount)} ${label.baseUnit}`,
            ...[
                [ru ? "Калории, ккал" : "Calories, kcal", label.calories],
                [ru ? "Белки, г" : "Protein, g", label.protein],
                [ru ? "Жиры, г" : "Fat, g", label.fat],
                [ru ? "Углеводы, г" : "Carbs, g", label.carbs],
                [ru ? "Клетчатка, г" : "Fiber, g", label.fiber],
                [ru ? "Алкоголь, г" : "Alcohol, g", label.alcohol],
            ].map(([name, value]) => `${name}: ${display(value)}`),
        ];
        const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="900" height="620" viewBox="0 0 900 620"><rect width="900" height="620" fill="white"/><g fill="black" font-family="Arial, sans-serif" font-size="30">${lines.map((line, index) => `<text x="40" y="${60 + index * 58}">${line}</text>`).join("")}</g></svg>\n`;
        writeFileSync(join(directory, item.input.image), svg);
    }
    writeFileSync(
        join(directory, "cases.json"),
        `${JSON.stringify(corpus, null, 2)}\n`,
    );
    return corpus.coverage;
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
)
    console.log(JSON.stringify(writeReferenceCorpus()));
