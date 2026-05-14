import { FormControl, FormGroup } from '@angular/forms';

import type {
    NutritionControlNames,
    NutritionMacroState,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { CHART_COLORS } from '../../../../../constants/chart-colors';
import { NUTRIENT_ROUNDING_FACTOR, PERCENT_MULTIPLIER } from '../../../../../shared/lib/nutrition.constants';
import { calculateMacroBarState } from '../../../../../shared/lib/nutrition-form.utils';
import { normalizeQualityScore } from '../../../../../shared/lib/quality-score.utils';
import type { Recipe } from '../../../models/recipe.data';
import {
    RECIPE_DETAIL_INGREDIENT_PREVIEW_LIMIT,
    RECIPE_DETAIL_MACRO_SUMMARY_LIMIT,
    RECIPE_DETAIL_MIN_MACRO_BAR_PERCENT,
} from './recipe-detail.config';
import type { IngredientPreviewItem, MacroBlock } from './recipe-detail.types';

const MIN_MACRO_REFERENCE_VALUE = 1;

export type RecipeDetailNutritionForm = {
    calories: FormControl<number | null>;
    proteins: FormControl<number | null>;
    fats: FormControl<number | null>;
    carbs: FormControl<number | null>;
    fiber: FormControl<number | null>;
    alcohol: FormControl<number | null>;
};

export type RecipeDetailViewModel = {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
    qualityScore: number;
    qualityGrade: string;
    macroBlocks: MacroBlock[];
    macroSummaryBlocks: MacroBlock[];
    ingredientPreview: IngredientPreviewItem[];
    nutritionControlNames: NutritionControlNames;
    nutritionForm: FormGroup<RecipeDetailNutritionForm>;
    macroBarState: NutritionMacroState;
    totalTime: number | null;
    ingredientCount: number;
};

export function buildRecipeDetailViewModel(recipe: Recipe, unknownIngredientName: string): RecipeDetailViewModel {
    const calories = resolveNutrientValue(recipe.totalCalories, recipe.manualCalories);
    const proteins = resolveNutrientValue(recipe.totalProteins, recipe.manualProteins);
    const fats = resolveNutrientValue(recipe.totalFats, recipe.manualFats);
    const carbs = resolveNutrientValue(recipe.totalCarbs, recipe.manualCarbs);
    const fiber = resolveFiberValue(recipe);
    const alcohol = resolveAlcoholValue(recipe);
    const macroReferenceValues = [proteins, fats, carbs];
    const macroBlocks = buildMacroBlocks({ proteins, fats, carbs, fiber, alcohol }, macroReferenceValues);

    return {
        calories,
        proteins,
        fats,
        carbs,
        fiber,
        alcohol,
        qualityScore: normalizeQualityScore(recipe.qualityScore),
        qualityGrade: recipe.qualityGrade ?? 'yellow',
        macroBlocks,
        macroSummaryBlocks: macroBlocks.slice(0, RECIPE_DETAIL_MACRO_SUMMARY_LIMIT),
        ingredientPreview: buildIngredientPreview(recipe, unknownIngredientName),
        nutritionControlNames: {
            calories: 'calories',
            proteins: 'proteins',
            fats: 'fats',
            carbs: 'carbs',
            fiber: 'fiber',
            alcohol: 'alcohol',
        },
        nutritionForm: buildNutritionForm({ calories, proteins, fats, carbs, fiber, alcohol }),
        macroBarState: calculateMacroBarState(proteins, fats, carbs),
        totalTime: calculateTotalPreparationTime(recipe),
        ingredientCount: computeIngredientCount(recipe),
    };
}

function buildMacroBlocks(
    values: { proteins: number; fats: number; carbs: number; fiber: number; alcohol: number },
    referenceValues: number[],
): MacroBlock[] {
    return [
        buildMacroBlock('GENERAL.NUTRIENTS.PROTEIN', values.proteins, CHART_COLORS.proteins, referenceValues),
        buildMacroBlock('GENERAL.NUTRIENTS.FAT', values.fats, CHART_COLORS.fats, referenceValues),
        buildMacroBlock('GENERAL.NUTRIENTS.CARB', values.carbs, CHART_COLORS.carbs, referenceValues),
        buildMacroBlock('GENERAL.NUTRIENTS.FIBER', values.fiber, CHART_COLORS.fiber, referenceValues),
        buildMacroBlock('GENERAL.NUTRIENTS.ALCOHOL', values.alcohol, CHART_COLORS.alcohol, referenceValues),
    ];
}

function buildMacroBlock(labelKey: string, value: number, color: string, referenceValues: number[]): MacroBlock {
    return {
        labelKey,
        value,
        unitKey: 'GENERAL.UNITS.G',
        color,
        percent: resolveMacroPercent(value, referenceValues),
    };
}

function buildNutritionForm(values: {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
}): FormGroup<RecipeDetailNutritionForm> {
    return new FormGroup<RecipeDetailNutritionForm>({
        calories: new FormControl(values.calories),
        proteins: new FormControl(values.proteins),
        fats: new FormControl(values.fats),
        carbs: new FormControl(values.carbs),
        fiber: new FormControl(values.fiber),
        alcohol: new FormControl(values.alcohol),
    });
}

function resolveMacroPercent(value: number, values: number[]): number {
    const max = Math.max(...values, value, MIN_MACRO_REFERENCE_VALUE);
    return Math.max(RECIPE_DETAIL_MIN_MACRO_BAR_PERCENT, Math.round((value / max) * PERCENT_MULTIPLIER));
}

function buildIngredientPreview(recipe: Recipe, unknownIngredientName: string): IngredientPreviewItem[] {
    return recipe.steps
        .flatMap(step => step.ingredients)
        .slice(0, RECIPE_DETAIL_INGREDIENT_PREVIEW_LIMIT)
        .map(ingredient => ({
            name: ingredient.productName ?? ingredient.nestedRecipeName ?? unknownIngredientName,
            amount: ingredient.amount,
            unitKey:
                ingredient.productBaseUnit !== null && ingredient.productBaseUnit !== undefined && ingredient.productBaseUnit.length > 0
                    ? `GENERAL.UNITS.${ingredient.productBaseUnit}`
                    : null,
        }));
}

function resolveFiberValue(recipe: Recipe): number {
    return recipe.totalFiber ?? recipe.manualFiber ?? computeFiberFromSteps(recipe) ?? 0;
}

function resolveAlcoholValue(recipe: Recipe): number {
    return recipe.totalAlcohol ?? recipe.manualAlcohol ?? computeAlcoholFromSteps(recipe) ?? 0;
}

function calculateTotalPreparationTime(recipe: Recipe): number | null {
    const hasPrep = recipe.prepTime !== null && recipe.prepTime !== undefined;
    const hasCook = recipe.cookTime !== null && recipe.cookTime !== undefined;

    if (!hasPrep && !hasCook) {
        return null;
    }

    const prep = recipe.prepTime ?? 0;
    const cook = recipe.cookTime ?? 0;
    const total = prep + cook;

    if (hasPrep && hasCook) {
        return total;
    }

    return hasPrep ? prep : cook;
}

function computeFiberFromSteps(recipe: Recipe): number | null {
    return computeNutrientFromSteps(recipe, ingredient => ingredient.productFiberPerBase);
}

function computeAlcoholFromSteps(recipe: Recipe): number | null {
    return computeNutrientFromSteps(recipe, ingredient => ingredient.productAlcoholPerBase);
}

function computeNutrientFromSteps(
    recipe: Recipe,
    resolveNutrientPerBase: (ingredient: Recipe['steps'][number]['ingredients'][number]) => number | null | undefined,
): number | null {
    if (recipe.steps.length === 0) {
        return null;
    }

    let total = 0;
    let hasValue = false;

    for (const step of recipe.steps) {
        for (const ingredient of step.ingredients) {
            const nutrientPerBase = resolveNutrientPerBase(ingredient);
            const baseAmount = ingredient.productBaseAmount;
            if (
                nutrientPerBase === null ||
                nutrientPerBase === undefined ||
                baseAmount === null ||
                baseAmount === undefined ||
                baseAmount === 0
            ) {
                continue;
            }

            total += nutrientPerBase * (ingredient.amount / baseAmount);
            hasValue = true;
        }
    }

    return hasValue ? Math.round(total * NUTRIENT_ROUNDING_FACTOR) / NUTRIENT_ROUNDING_FACTOR : null;
}

function computeIngredientCount(recipe: Recipe): number {
    return recipe.steps.reduce((total, step) => total + step.ingredients.length, 0);
}

function resolveNutrientValue(value?: number | null, manual?: number | null): number {
    return manual ?? value ?? 0;
}
