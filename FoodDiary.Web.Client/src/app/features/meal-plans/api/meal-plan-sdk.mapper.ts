import type { MealPlanHttpResponse } from '../../../shared/api/sdk/generated/model/meal-plan-http-response';
import type { MealPlanSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/meal-plan-summary-http-response';
import { requireSdkFields, sdkEnum } from '../../../shared/api/sdk/sdk-response';
import { DIET_TYPES, type MealPlan, type MealPlanSummary } from '../models/meal-plan.data';

export function mealPlanSummaryFromSdk(response: MealPlanSummaryHttpResponse): MealPlanSummary {
    const value = requireSdkFields(response, ['id', 'name', 'dietType', 'durationDays', 'isCurated', 'totalRecipes']);
    return {
        ...value,
        dietType: sdkEnum(
            value.dietType,
            DIET_TYPES.map(item => item.value),
        ),
    };
}

export function mealPlanFromSdk(response: MealPlanHttpResponse): MealPlan {
    const value = requireSdkFields(response, ['id', 'name', 'dietType', 'durationDays', 'isCurated', 'days']);
    return {
        ...value,
        dietType: sdkEnum(
            value.dietType,
            DIET_TYPES.map(item => item.value),
        ),
        days: value.days.map(dayResponse => {
            const day = requireSdkFields(dayResponse, ['id', 'dayNumber', 'meals']);
            return {
                ...day,
                meals: day.meals.map(meal => requireSdkFields(meal, ['id', 'mealType', 'recipeId', 'servings'])),
            };
        }),
    };
}
