import { type Recipe, RecipeVisibility } from '../../models/recipe.data';
import { RECIPE_CATEGORIES } from '../../models/recipe-category';
import type { RecipeLookup } from '../../models/recipe-lookup.data';
import type { RecipeHttpResponse } from './generated/model/recipe-http-response';
import { requireSdkFields, sdkEnum, sdkOptional } from './sdk-response';

export function recipeFromSdk(response: RecipeHttpResponse): Recipe {
    const value = requireSdkFields(response, [
        'id',
        'name',
        'servings',
        'visibility',
        'usageCount',
        'createdAt',
        'isOwnedByCurrentUser',
        'isNutritionAutoCalculated',
        'steps',
    ]);
    return {
        ...value,
        language: value.language ?? undefined,
        visibility: sdkEnum(value.visibility, Object.values(RecipeVisibility)),
        category: value.category === undefined ? undefined : sdkOptional(value.category, category => sdkEnum(category, RECIPE_CATEGORIES)),
        qualityGrade:
            value.qualityGrade === undefined
                ? undefined
                : sdkOptional(value.qualityGrade, grade => sdkEnum(grade, ['green', 'yellow', 'red'] as const)),
        images: value.images?.map(image => requireSdkFields(image, ['imageAssetId', 'imageUrl'])),
        steps: value.steps.map(stepResponse => {
            const step = requireSdkFields(stepResponse, ['id', 'stepNumber', 'instruction', 'ingredients']);
            return {
                ...step,
                images: step.images?.map(image => requireSdkFields(image, ['imageAssetId', 'imageUrl'])),
                ingredients: step.ingredients.map(ingredient => requireSdkFields(ingredient, ['id', 'amount'])),
            };
        }),
    };
}

/** Serving-mass lookup needs only this published subset of the recipe. */
export function recipeLookupFromSdk(response: RecipeHttpResponse): RecipeLookup {
    const value = requireSdkFields(response, ['id', 'servings', 'steps']);
    return {
        id: value.id,
        servings: value.servings,
        steps: value.steps.map(step => ({
            ingredients: requireSdkFields(step, ['ingredients']).ingredients.map(ingredient => ({
                amount: requireSdkFields(ingredient, ['amount']).amount,
                productBaseUnit: ingredient.productBaseUnit ?? null,
            })),
        })),
    };
}
