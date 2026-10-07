import type { FavoriteMeal } from '../../models/meal.data';
import type { FavoriteRecipe } from '../../models/recipe.data';
import type { FavoriteMealHttpResponse } from './generated/model/favorite-meal-http-response';
import type { FavoriteRecipeHttpResponse } from './generated/model/favorite-recipe-http-response';
import { requireSdkFields } from './sdk-response';

export function favoriteMealFromSdk(response: FavoriteMealHttpResponse): FavoriteMeal {
    const value = requireSdkFields(response, [
        'id',
        'mealId',
        'createdAtUtc',
        'mealDate',
        'totalCalories',
        'totalProteins',
        'totalFats',
        'totalCarbs',
        'itemCount',
    ]);
    return {
        ...value,
        name: value.name ?? null,
        mealType: value.mealType ?? null,
        itemNames: value.itemNames ?? undefined,
        itemImageUrls: value.itemImageUrls ?? undefined,
    };
}

export function favoriteRecipeFromSdk(response: FavoriteRecipeHttpResponse): FavoriteRecipe {
    const value = requireSdkFields(response, ['id', 'recipeId', 'createdAtUtc', 'recipeName', 'servings', 'ingredientCount']);
    return { ...value, ingredientNames: value.ingredientNames ?? undefined };
}
