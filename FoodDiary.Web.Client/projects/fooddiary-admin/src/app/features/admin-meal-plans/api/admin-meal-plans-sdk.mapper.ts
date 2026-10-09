import type { CatalogRecipeHttpResponse } from '../../../shared/api/sdk/generated/model/catalog-recipe-http-response';
import type { MealPlanDayHttpResponse } from '../../../shared/api/sdk/generated/model/meal-plan-day-http-response';
import type { MealPlanHttpResponse } from '../../../shared/api/sdk/generated/model/meal-plan-http-response';
import type { MealPlanMealHttpResponse } from '../../../shared/api/sdk/generated/model/meal-plan-meal-http-response';
import type { MealPlanSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/meal-plan-summary-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import type { CatalogDay, CatalogMeal, CatalogPlan, CatalogPlanSummary, CatalogRecipe } from '../models/admin-meal-plan.data';

export function mealPlanSummaryFromSdk(response: MealPlanSummaryHttpResponse): CatalogPlanSummary {
    const value = requireSdkFields(response, ['id', 'name', 'dietType', 'durationDays', 'isCurated']);
    return {
        ...value,
        description: value.description ?? null,
        targetCaloriesPerDay: value.targetCaloriesPerDay ?? null,
        id: adminId<'meal-plan'>(value.id),
    };
}

export function mealPlanFromSdk(response: MealPlanHttpResponse): CatalogPlan {
    const value = requireSdkFields(response, ['id', 'name', 'dietType', 'durationDays', 'isCurated', 'days']);
    return {
        ...value,
        description: value.description ?? null,
        targetCaloriesPerDay: value.targetCaloriesPerDay ?? null,
        days: value.days.map(item => mealPlanDayFromSdk(item)),

        id: adminId<'meal-plan'>(value.id),
    };
}

export function mealPlanDayFromSdk(response: MealPlanDayHttpResponse): CatalogDay {
    const value = requireSdkFields(response, ['dayNumber', 'meals']);
    return { ...value, meals: value.meals.map(item => mealPlanMealFromSdk(item)) };
}

export function mealPlanMealFromSdk(response: MealPlanMealHttpResponse): CatalogMeal {
    const value = requireSdkFields(response, ['mealType', 'recipeId', 'servings']);
    return { ...value, recipeId: adminId<'recipe'>(value.recipeId) };
}

export function catalogRecipeFromSdk(response: CatalogRecipeHttpResponse): CatalogRecipe {
    const value = requireSdkFields(response, ['id', 'name', 'servings']);
    return { ...value, id: adminId<'recipe'>(value.id) };
}
