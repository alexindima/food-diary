import type { MealPlanHttpResponse } from '../../../shared/api/sdk/generated/model/meal-plan-http-response';
import type { MealPlanSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/meal-plan-summary-http-response';
import { requireSdkFields, sdkEnum } from '../../../shared/api/sdk/sdk-response';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { DIET_TYPES, type MealPlan, type MealPlanSummary } from '../models/meal-plan.data';
import {
    planDayNumberFromStored,
    planDurationDaysFromStored,
    plannedMealTypeFromStored,
    plannedServingsFromStored,
} from '../models/meal-plan-values';

export function mealPlanSummaryFromSdk(response: MealPlanSummaryHttpResponse): MealPlanSummary {
    const value = requireSdkFields(response, ['id', 'name', 'dietType', 'durationDays', 'isCurated', 'totalRecipes']);
    return {
        ...value,
        id: entityId<'meal-plan'>(value.id),
        durationDays: planDurationDaysFromStored(value.durationDays),
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
        id: entityId<'meal-plan'>(value.id),
        durationDays: planDurationDaysFromStored(value.durationDays),
        dietType: sdkEnum(
            value.dietType,
            DIET_TYPES.map(item => item.value),
        ),
        days: value.days.map(dayResponse => {
            const day = requireSdkFields(dayResponse, ['id', 'dayNumber', 'meals']);
            return {
                ...day,
                id: entityId<'meal-plan-day'>(day.id),
                dayNumber: planDayNumberFromStored(day.dayNumber),
                meals: day.meals.map(responseMeal => {
                    const meal = requireSdkFields(responseMeal, ['id', 'mealType', 'recipeId', 'servings']);
                    return {
                        ...meal,
                        id: entityId<'meal-plan-meal'>(meal.id),
                        recipeId: entityId<'recipe'>(meal.recipeId),
                        servings: plannedServingsFromStored(meal.servings),
                        mealType: plannedMealTypeFromStored(meal.mealType),
                    };
                }),
            };
        }),
    };
}
