import { describe, expect, it } from 'vitest';

const TEST_PLAN_DURATION_DAYS = 7;

import { entityId } from '../../../shared/models/semantics/entity-id';
import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';
import { plannedServingsFromStored } from '../models/meal-plan-values';
import { plannedMealTypeFromStored } from '../models/meal-plan-values';
import { planDayNumberFromStored } from '../models/meal-plan-values';
import { planDurationDaysFromStored } from '../models/meal-plan-values';
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
                { unitKey: 'GENERAL.UNITS.G', value: 30, prefix: 'GENERAL.NUTRIENTS.PROTEIN' },
            ],
        });
    });

    it('returns null detail view for missing plan', () => {
        expect(buildMealPlanDetailView(null)).toBeNull();
    });
});

function createSummary(overrides: Partial<MealPlanSummary> = {}): MealPlanSummary {
    return {
        id: entityId<'meal-plan'>('plan-1'),
        name: 'Balanced plan',
        description: 'Plan description',
        dietType: 'Balanced',
        durationDays: planDurationDaysFromStored(TEST_PLAN_DURATION_DAYS),
        targetCaloriesPerDay: 2000,
        isCurated: true,
        totalRecipes: 21,
        ...overrides,
    };
}

function createMealPlan(): MealPlan {
    return {
        id: entityId<'meal-plan'>('plan-1'),
        name: 'Balanced plan',
        description: null,
        dietType: 'Balanced',
        durationDays: planDurationDaysFromStored(TEST_PLAN_DURATION_DAYS),
        targetCaloriesPerDay: 2000,
        isCurated: true,
        days: [
            {
                id: entityId<'meal-plan-day'>('day-1'),
                dayNumber: planDayNumberFromStored(1),
                meals: [
                    {
                        id: entityId<'meal-plan-meal'>('meal-1'),
                        mealType: plannedMealTypeFromStored('Breakfast'),
                        recipeId: entityId<'recipe'>('recipe-1'),
                        recipeName: 'Omelette',
                        servings: plannedServingsFromStored(1),
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
