import type { RecipeImportResult } from '../../../../../shared/models/recipe-import.data';
import type { RecipeFormValues } from './recipe-manage.types';
import { createRecipeIngredientValue, createRecipeStepValue } from './recipe-manage-form.mapper';

export function mapRecognizedRecipe(result: RecipeImportResult, sourceLabel: string, nutritionLabel: string): Partial<RecipeFormValues> {
    const stepDescriptions = result.steps.length > 0 ? result.steps : [''];
    const steps = stepDescriptions.map((description, index) => ({
        ...createRecipeStepValue(),
        description,
        ingredients:
            index === 0
                ? result.ingredients.map(item => createRecipeIngredientValue({ textName: item.name, amountText: item.amount }))
                : [],
    }));
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
        steps,
        calculateNutritionAutomatically: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
    };
}
