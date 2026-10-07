import { describe, expect, it } from 'vitest';

import { mealPlanFromSdk } from './meal-plan-sdk.mapper';

describe('Meal plan SDK nutrition', () => {
    it('keeps fractional servings, zero nutrients and absent meal-plan nutrition distinct', () => {
        const plan = mealPlanFromSdk({
            id: 'plan',
            name: 'Plan',
            dietType: 'Balanced',
            durationDays: 1,
            isCurated: false,
            days: [
                {
                    id: 'day',
                    dayNumber: 1,
                    meals: [
                        {
                            id: 'meal',
                            mealType: 'Lunch',
                            recipeId: 'recipe',
                            servings: 0.75,
                            calories: 333.33,
                            proteins: 0,
                            fats: null,
                            carbs: 12.345,
                        },
                    ],
                },
            ],
        });
        expect(plan.days[0].meals[0]).toMatchObject({ servings: 0.75, calories: 333.33, proteins: 0, fats: null, carbs: 12.345 });
    });
});
