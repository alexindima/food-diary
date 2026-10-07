import type { PublicRecipeHttpResponse } from '../../../shared/api/sdk/generated/model/public-recipe-http-response';
import { requireSdkFields, sdkEnum, sdkNullableFields, sdkOptional } from '../../../shared/api/sdk/sdk-response';
import { RECIPE_CATEGORIES } from '../../../shared/models/recipe-category';
import type { PublicRecipe } from '../models/public-recipe.data';

export function publicRecipeFromSdk(response: PublicRecipeHttpResponse): PublicRecipe {
    const value = requireSdkFields(response, ['id', 'name', 'images', 'servings', 'missingIngredientCount', 'steps']);
    return {
        ...sdkNullableFields(value, [
            'description',
            'imageUrl',
            'prepTime',
            'cookTime',
            'totalCalories',
            'totalProteins',
            'totalFats',
            'totalCarbs',
            'totalFiber',
            'totalAlcohol',
        ]),
        language: value.language ?? undefined,
        category: sdkOptional(value.category, category => sdkEnum(category, RECIPE_CATEGORIES)),
        missingIngredientNames: value.missingIngredientNames ?? undefined,
        steps: value.steps.map(stepResponse => {
            const step = requireSdkFields(stepResponse, ['stepNumber', 'instruction', 'images', 'ingredients']);
            return {
                ...step,
                title: step.title ?? null,
                ingredients: step.ingredients.map(ingredient =>
                    sdkNullableFields(requireSdkFields(ingredient, ['isAvailable']), ['name', 'amount', 'unit', 'amountText', 'recipeId']),
                ),
            };
        }),
    };
}
