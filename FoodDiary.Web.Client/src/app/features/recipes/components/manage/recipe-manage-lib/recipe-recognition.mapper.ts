import { MANUAL_NUTRITION_MAX_CALORIES, MANUAL_NUTRITION_MAX_NUTRIENT } from '../../../../../shared/lib/nutrition.constants';
import type { RecipeImportResult } from '../../../../../shared/models/recipe-import.data';
import type { RecipeFormValues } from './recipe-manage.types';
import { createRecipeIngredientValue, createRecipeStepValue } from './recipe-manage-form.mapper';

export function mapRecognizedRecipe(result: RecipeImportResult, sourceLabel: string, nutritionLabel: string): Partial<RecipeFormValues> {
    const notes = [
        result.sourceUrl === null ? null : `${sourceLabel}: ${result.sourceUrl}`,
        result.authorNutrition === null ? null : `${nutritionLabel}: ${result.authorNutrition}`,
    ].filter((note): note is string => note !== null);
    return {
        name: result.name,
        description: result.description,
        comment: notes.length > 0 ? notes.join('\n\n') : null,
        prepTime: result.prepMinutes,
        cookTime: result.cookMinutes === 0 ? null : result.cookMinutes,
        servings: result.servings ?? 1,
        steps: recognizedSteps(result),
        ...manualNutritionFields(result),
    };
}

function recognizedSteps(result: RecipeImportResult): RecipeFormValues['steps'] {
    const descriptions = result.steps.length > 0 ? result.steps : [''];
    const steps = descriptions.map(description => ({ ...createRecipeStepValue(), description }));
    result.ingredients.forEach(item => {
        const index = item.stepIndex;
        const stepIndex = typeof index === 'number' && Number.isInteger(index) && index >= 0 && index < steps.length ? index : 0;
        steps[stepIndex]?.ingredients.push(createRecipeIngredientValue({ textName: item.name, amountText: item.amount }));
    });
    return steps;
}

function manualNutritionFields(result: RecipeImportResult): Partial<RecipeFormValues> {
    const nutrition = statedRecipeNutrition(result);
    if (nutrition === null) {
        return {
            calculateNutritionAutomatically: true,
            manualCalories: null,
            manualProteins: null,
            manualFats: null,
            manualCarbs: null,
            manualFiber: null,
            manualAlcohol: null,
        };
    }
    return {
        calculateNutritionAutomatically: false,
        manualCalories: nutrition.calories,
        manualProteins: nutrition.proteins,
        manualFats: nutrition.fats,
        manualCarbs: nutrition.carbs,
        manualFiber: nutrition.fiber,
        manualAlcohol: nutrition.alcohol,
    };
}

function statedRecipeNutrition(result: RecipeImportResult): Omit<NonNullable<RecipeImportResult['nutrition']>, 'basis'> | null {
    const nutrition = result.nutrition;
    if (nutrition === null || nutrition === undefined) {
        return null;
    }
    const multiplier = recipeNutritionMultiplier(nutrition.basis, result.servings);
    if (multiplier === null || [nutrition.calories, nutrition.proteins, nutrition.fats, nutrition.carbs].includes(null)) {
        return null;
    }
    const totals = {
        calories: scaleStatedNutrient(nutrition.calories, multiplier),
        proteins: scaleStatedNutrient(nutrition.proteins, multiplier),
        fats: scaleStatedNutrient(nutrition.fats, multiplier),
        carbs: scaleStatedNutrient(nutrition.carbs, multiplier),
        fiber: scaleStatedNutrient(nutrition.fiber, multiplier),
        alcohol: scaleStatedNutrient(nutrition.alcohol, multiplier),
    };
    const values = [totals.calories, totals.proteins, totals.fats, totals.carbs, totals.fiber, totals.alcohol];
    const valid = values.every(
        (value, index) =>
            value === null ||
            (Number.isFinite(value) &&
                value >= 0 &&
                value <= (index === 0 ? MANUAL_NUTRITION_MAX_CALORIES : MANUAL_NUTRITION_MAX_NUTRIENT)),
    );
    return valid ? totals : null;
}

function recipeNutritionMultiplier(basis: NonNullable<RecipeImportResult['nutrition']>['basis'], servings: number | null): number | null {
    if (basis === 'recipe') {
        return 1;
    }
    return basis === 'serving' && servings !== null && Number.isInteger(servings) && servings > 0 ? servings : null;
}

function scaleStatedNutrient(value: number | null, multiplier: number): number | null {
    return value === null ? null : value * multiplier;
}
