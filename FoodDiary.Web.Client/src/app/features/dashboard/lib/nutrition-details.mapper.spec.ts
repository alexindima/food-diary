import { describe, expect, it } from 'vitest';

import { nutritionMealFixture, nutritionSnapshotFixture } from '../../../../testing/nutrition-details.fixtures';
import { buildNutritionSources, nutritionDayProgress } from './nutrition-details.mapper';

const LARGER_CALORIES = 500;
const SMALLER_CALORIES = 200;

describe('Nutrition source attribution', () => {
    registerPortionTests();
    registerAiTests();
    registerFallbackTests();
});

function registerPortionTests(): void {
    it('scales product base quantities and fractional recipe servings using the saved snapshots', () => {
        const meal = nutritionMealFixture({
            totalProteins: 17.5,
            items: [
                {
                    id: 'product-item',
                    mealId: 'meal-1',
                    productId: 'product-1',
                    productName: 'Milk',
                    productBaseAmount: 100,
                    productProteinsPerBase: 20,
                    amount: 50,
                },
                {
                    id: 'recipe-item',
                    mealId: 'meal-1',
                    recipeId: 'recipe-1',
                    recipeName: 'Soup',
                    recipeServings: 4,
                    recipeTotalProteins: 20,
                    amount: 1.5,
                },
            ],
        });
        expect(buildNutritionSources([meal], 'protein', 'en').map(({ name, value, kind }) => ({ name, value, kind }))).toEqual([
            { name: 'Milk', value: 10, kind: 'product' },
            { name: 'Soup', value: 7.5, kind: 'recipe' },
        ]);
    });

    it('keeps a manual total separate from product attribution', () => {
        const meal = nutritionMealFixture({
            isNutritionAutoCalculated: false,
            manualProteins: 45,
            totalProteins: 45,
            items: [
                {
                    id: 'item',
                    mealId: 'meal-1',
                    productId: 'product',
                    productName: 'Milk',
                    productBaseAmount: 100,
                    productProteinsPerBase: 20,
                    amount: 50,
                },
            ],
        });
        expect(buildNutritionSources([meal], 'protein', 'en')).toEqual([{ id: meal.id, meal, name: null, value: 45, kind: 'manual' }]);
    });
}

function registerAiTests(): void {
    it('includes accepted AI consumed totals without multiplying them by food mass and ignores rejected items', () => {
        const meal = nutritionMealFixture({
            totalProteins: 7,
            aiSessions: [
                {
                    id: 'session',
                    mealId: 'meal-1',
                    recognizedAtUtc: '2026-10-28T09:00:00Z',
                    items: [
                        {
                            id: 'accepted',
                            sessionId: 'session',
                            nameEn: 'Chicken',
                            nameLocal: 'Курица',
                            amount: 90,
                            unit: 'g',
                            calories: 100,
                            proteins: 7,
                            fats: 0,
                            carbs: 0,
                            fiber: 0,
                            alcohol: 0,
                            resolution: 'Accepted',
                        },
                        {
                            id: 'rejected',
                            sessionId: 'session',
                            nameEn: 'Rejected',
                            amount: 50,
                            unit: 'g',
                            calories: 100,
                            proteins: 20,
                            fats: 0,
                            carbs: 0,
                            fiber: 0,
                            alcohol: 0,
                            resolution: 'Rejected',
                        },
                    ],
                },
            ],
        });
        expect(buildNutritionSources([meal], 'protein', 'ru')).toMatchObject([{ name: 'Курица', value: 7, kind: 'ai' }]);
    });
}

function registerFallbackTests(): void {
    it('does not invent a product breakdown when the snapshot is incomplete or disagrees with the recorded total', () => {
        const meal = nutritionMealFixture({
            totalProteins: 12,
            items: [
                {
                    id: 'item',
                    mealId: 'meal-1',
                    productId: 'product',
                    productName: 'Milk',
                    productBaseAmount: 100,
                    productProteinsPerBase: 20,
                    amount: 50,
                },
            ],
        });
        expect(buildNutritionSources([meal], 'protein', 'en')).toMatchObject([{ kind: 'unavailable', value: 12, name: null }]);
        expect(
            buildNutritionSources([{ ...meal, items: [{ ...meal.items[0], product: null, recipe: null }] }], 'protein', 'en'),
        ).toMatchObject([{ kind: 'unavailable', value: 12 }]);
    });

    it('shows recorded calories rather than deriving them from macros, and sorts meals by contribution', () => {
        const meals = [nutritionMealFixture(), nutritionMealFixture({ id: 'meal-2', totalCalories: 500, totalProteins: 0 })];
        expect(buildNutritionSources(meals, 'calories', 'en').map(source => source.value)).toEqual([LARGER_CALORIES, SMALLER_CALORIES]);
    });

    it('keeps missing goals absent and supports an empty recorded day', () => {
        expect(nutritionDayProgress(nutritionSnapshotFixture(), 'fiber')).toEqual({ current: 0, goal: null });
        expect(buildNutritionSources([nutritionMealFixture({ totalProteins: 0 })], 'protein', 'en')).toEqual([]);
    });
}
