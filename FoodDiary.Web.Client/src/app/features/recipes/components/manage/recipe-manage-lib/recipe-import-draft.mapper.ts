import type { IngredientFormValues, StepFormValues } from './recipe-manage.types';
import { createRecipeStepValue } from './recipe-manage-form.mapper';

export type RecipeImportDraft = {
    comment: string | null;
    description: string | null;
    name: string;
    steps: StepFormValues[];
};

type RecipeImportDraftInput = {
    existingIngredients: readonly IngredientFormValues[];
    ingredientsLabel: string;
    sourceLabel: string;
    sourceUrl: string;
    text: string;
};

type RecipeImportSections = {
    ingredients: string[];
    notes: string[];
    steps: string[];
};

const sectionHeadings: Readonly<Partial<Record<string, keyof RecipeImportSections>>> = {
    ingredients: 'ingredients',
    ингредиенты: 'ingredients',
    steps: 'steps',
    instructions: 'steps',
    method: 'steps',
    шаги: 'steps',
    приготовление: 'steps',
};

export function parseRecipeImportDraft(input: RecipeImportDraftInput): RecipeImportDraft | null {
    const sourceUrl = input.sourceUrl.trim();
    const lines = getImportLines(input.text);

    if (lines.length === 0 && sourceUrl.length === 0) {
        return null;
    }

    const name = lines[0] ?? sourceUrl;
    const contentLines = name === sourceUrl ? lines : lines.slice(1);
    const sections = splitImportSections(contentLines);
    const description = buildImportDescription(sections.ingredients, sections.notes, input.ingredientsLabel);
    const stepLines = sections.steps.length > 0 ? sections.steps : sections.notes;

    return {
        name,
        description,
        comment: sourceUrl.length > 0 ? `${input.sourceLabel}: ${sourceUrl}` : null,
        steps: buildImportSteps(stepLines, name, input.existingIngredients),
    };
}

function getImportLines(text: string): string[] {
    return text
        .split(/\r?\n/)
        .map(line => line.trim())
        .filter(line => line.length > 0);
}

function splitImportSections(lines: readonly string[]): RecipeImportSections {
    const sections: RecipeImportSections = { ingredients: [], steps: [], notes: [] };
    let activeSection: keyof RecipeImportSections = 'notes';

    for (const line of lines) {
        const heading = sectionHeadings[line.toLowerCase().replace(/:$/, '')];
        if (heading !== undefined) {
            activeSection = heading;
            continue;
        }

        sections[activeSection].push(line);
    }

    return sections;
}

function buildImportDescription(ingredients: readonly string[], notes: readonly string[], ingredientsLabel: string): string | null {
    const parts = [...notes];

    if (ingredients.length > 0) {
        parts.push(`${ingredientsLabel}\n${ingredients.join('\n')}`);
    }

    return parts.length > 0 ? parts.join('\n\n') : null;
}

function buildImportSteps(
    stepLines: readonly string[],
    fallback: string,
    existingIngredients: readonly IngredientFormValues[],
): StepFormValues[] {
    const lines = stepLines.length > 0 ? stepLines : [fallback];

    return lines.map(line => {
        const step = createRecipeStepValue();

        return {
            ...step,
            description: stripStepPrefix(line),
            ingredients: existingIngredients.length > 0 ? existingIngredients.map(ingredient => ({ ...ingredient })) : step.ingredients,
        };
    });
}

function stripStepPrefix(value: string): string {
    return value.replace(/^\s*(?:[-*]|\d+[.)])\s*/, '').trim();
}
