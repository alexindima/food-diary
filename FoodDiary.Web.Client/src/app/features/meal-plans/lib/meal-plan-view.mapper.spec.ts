import { describe, expect, it } from 'vitest';

import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';
import { buildMealPlanCards, buildMealPlanDetailView, buildMealPlanDietFilterOptions } from './meal-plan-view.mapper';

describe('meal plan view mapper', () => {
    it('builds diet filter options with selected fill state', () => {
        const options = buildMealPlanDietFilterOptions('Keto');

        expect(options[0]).toEqual({
            value: null,
            labelKey: 'MEAL_PLANS.FILTER_ALL',
            fill: 'outline',
        });
        expect(options.find(option => option.value === 'Keto')?.fill).toBe('solid');
        expect(options.find(option => option.value === 'Balanced')?.fill).toBe('outline');
    });

    it('builds list card translation keys', () => {
        const cards = buildMealPlanCards([createSummary({ dietType: 'HighProtein' })]);

        expect(cards[0]).toMatchObject({
            id: 'plan-1',
            dietTypeKey: 'MEAL_PLANS.DIET_TYPE.HIGHPROTEIN',
        });
    });

    it('builds detail view and filters empty nutrition values', () => {
        const view = buildMealPlanDetailView(createMealPlan());

        expect(view?.header).toEqual({
            dietTypeKey: 'MEAL_PLANS.DIET_TYPE.BALANCED',
            name: 'Balanced plan',
            description: null,
            isCurated: true,
        });
        expect(view?.days[0].meals[0]).toMatchObject({
            mealTypeKey: 'MEAL_PLANS.MEAL_TYPE.BREAKFAST',
            nutritionItems: [
                { unitKey: 'GENERAL.UNITS.KCAL', value: 450, prefix: '' },
                { unitKey: 'GENERAL.UNITS.G', value: 30, prefix: 'P: ' },
            ],
        });
    });

    it('returns null detail view for missing plan', () => {
        expect(buildMealPlanDetailView(null)).toBeNull();
    });
});

function createSummary(overrides: Partial<MealPlanSummary> = {}): MealPlanSummary {
    return {
        id: 'plan-1',
        name: 'Balanced plan',
        description: 'Plan description',
        dietType: 'Balanced',
        durationDays: 7,
        targetCaloriesPerDay: 2000,
        isCurated: true,
        totalRecipes: 21,
        ...overrides,
    };
}

function createMealPlan(): MealPlan {
    return {
        id: 'plan-1',
        name: 'Balanced plan',
        description: null,
        dietType: 'Balanced',
        durationDays: 7,
        targetCaloriesPerDay: 2000,
        isCurated: true,
        days: [
            {
                id: 'day-1',
                dayNumber: 1,
                meals: [
                    {
                        id: 'meal-1',
                        mealType: 'Breakfast',
                        recipeId: 'recipe-1',
                        recipeName: 'Omelette',
                        servings: 1,
                        calories: 450,
                        proteins: 30,
                        fats: 0,
                        carbs: null,
                    },
                ],
            },
        ],
    };
}
