import { dashboardSnapshotFromSdk } from '../app/shared/api/sdk/dashboard-sdk.mapper';
import type { MealHttpResponse } from '../app/shared/api/sdk/generated/model/meal-http-response';
import { mealFromSdk } from '../app/shared/api/sdk/meal-sdk.mapper';
import type { DashboardSnapshot } from '../app/shared/models/dashboard.data';
import type { Meal } from '../app/shared/models/meal.data';

export function nutritionMealFixture(overrides: Partial<MealHttpResponse> = {}): Meal {
    return mealFromSdk({
        id: 'meal-1',
        date: '2026-10-28T09:00:00Z',
        mealType: 'BREAKFAST',
        totalCalories: 200,
        totalProteins: 10,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        items: [],
        ...overrides,
    });
}

export function nutritionSnapshotFixture(meals: Meal[] = [], overrides: Partial<DashboardSnapshot> = {}): DashboardSnapshot {
    const snapshot = dashboardSnapshotFromSdk({
        date: '2026-10-28T00:00:00Z',
        dateTo: '2026-10-28T23:59:59Z',
        dailyGoal: 2000,
        weeklyCalorieGoal: 14000,
        statistics: { totalCalories: 200, averageProteins: 10, averageFats: 0, averageCarbs: 0, averageFiber: 0, proteinGoal: 100 },
        weeklyCalories: [],
        meals: { items: [], total: 0 },
        weight: { latest: null, previous: null, desiredWeightKg: null },
        waist: { latest: null, previous: null, desiredWaistCm: null },
    });
    return { ...snapshot, meals: { items: meals, total: meals.length }, ...overrides };
}
