import { describe, expect, it } from 'vitest';

import { nutritionMealFixture, nutritionSnapshotFixture } from '../../../../testing/nutrition-details.fixtures';
import { createEmptyRecipeSnapshot, mealItemFromStored } from '../../../shared/models/meal.data';
import { entityId } from '../../../shared/models/semantics/entity-id';
import {
    buildNutritionSources,
    hasMatchingNutritionTotal,
    nutritionDayProgress,
    nutritionMetricPresentation,
} from './nutrition-details.mapper';

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
    it.each([
        ['ru', 'Курица', 'Курица'],
        ['ru', null, 'Chicken'],
        ['en', 'Курица', 'Chicken'],
    ] as const)('uses the recorded AI totals and the selected language %s with translation %s', (language, nameLocal, expectedName) => {
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
                            nameLocal,
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
        expect(buildNutritionSources([meal], 'protein', language)).toMatchObject([{ name: expectedName, value: 7, kind: 'ai' }]);
    });
}

describe('Daily nutrition metric presentation', () => {
    it.each([
        ['calories', { current: 875, goal: 2000 }, 'MEAL_RING.UNIT'],
        ['protein', { current: 35, goal: null }, 'GENERAL.UNITS.G'],
        ['fats', { current: 25, goal: 60 }, 'GENERAL.UNITS.G'],
        ['carbs', { current: 85, goal: null }, 'GENERAL.UNITS.G'],
    ] as const)('uses recorded daily progress and units for %s', (metric, expectedProgress, unitKey) => {
        const snapshot = nutritionSnapshotFixture([], {
            statistics: { totalCalories: 875, averageProteins: 35, averageFats: 25, averageCarbs: 85, averageFiber: 5, fatGoal: 60 },
        });

        expect(nutritionDayProgress(snapshot, metric)).toEqual(expectedProgress);
        expect(nutritionMetricPresentation(metric).unitKey).toBe(unitKey);
    });

    it('shows recorded fat progress when a legacy daily snapshot has no fat goal', () => {
        const recordedFat = 25;
        const snapshot = nutritionSnapshotFixture([], {
            statistics: { totalCalories: 875, averageProteins: 35, averageFats: recordedFat, averageCarbs: 85, averageFiber: 5 },
        });

        expect(nutritionDayProgress(snapshot, 'fats')).toEqual({ current: recordedFat, goal: null });
    });

    it('keeps manual calorie totals separate even if a recorded product is present', () => {
        const meal = nutritionMealFixture({
            totalCalories: 340,
            isNutritionAutoCalculated: false,
            manualCalories: 340,
            items: [{ id: 'item', mealId: 'meal-1', productId: 'product', productName: 'Milk', productCaloriesPerBase: 50, amount: 100 }],
        });

        expect(buildNutritionSources([meal], 'calories', 'en')).toMatchObject([{ kind: 'manual', value: 340, name: null }]);
    });

    it('keeps the recorded meal total when a historical product has no usable base amount', () => {
        const meal = nutritionMealFixture({
            totalFats: 8,
            items: [
                {
                    id: 'item',
                    mealId: 'meal-1',
                    productId: 'product',
                    productName: 'Milk',
                    productFatsPerBase: 4,
                    productBaseAmount: 0,
                    amount: 100,
                },
            ],
        });

        expect(buildNutritionSources([meal], 'fats', 'en')).toMatchObject([{ kind: 'unavailable', value: 8, name: null }]);
    });
});

describe('Daily nutrition consistency', () => {
    it('accepts the rounded dashboard total for a more precise recorded meal', () => {
        const meals = [nutritionMealFixture({ totalProteins: 0.105 })];
        const dashboardTotal = 0.1;
        const outdatedTotal = 0.09;

        expect(hasMatchingNutritionTotal(meals, 'protein', dashboardTotal)).toBe(true);
        expect(hasMatchingNutritionTotal(meals, 'protein', outdatedTotal)).toBe(false);
    });
});

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

    it('attributes the saved rounded total to a product when an older meal has no AI sessions', () => {
        const savedFatTotal = 4.13;
        const servedAmount = 33;
        const fatsPerBase = 12.5;
        const meal = {
            ...nutritionMealFixture({
                totalFats: savedFatTotal,
                items: [
                    {
                        id: 'historical-product',
                        mealId: 'meal-1',
                        productId: 'saved-product',
                        productName: 'Hard cheese',
                        productBaseAmount: 100,
                        productFatsPerBase: fatsPerBase,
                        amount: servedAmount,
                    },
                ],
            }),
            aiSessions: undefined,
        };

        expect(buildNutritionSources([meal], 'fats', 'en')).toMatchObject([{ kind: 'product', name: 'Hard cheese', value: 4.125 }]);
    });
}

describe('Historical recipe nutrition attribution', () => {
    const savedProtein = 10;
    const meal = nutritionMealFixture({
        totalProteins: savedProtein,
        items: [
            {
                id: 'product-item',
                mealId: 'meal-1',
                productId: 'milk',
                productName: 'Milk',
                productBaseAmount: 100,
                productProteinsPerBase: savedProtein,
                amount: 100,
            },
        ],
    });

    it.each([
        { total: null, expected: { kind: 'unavailable', name: null } },
        { total: undefined, expected: { kind: 'unavailable', name: null } },
        { total: 0, expected: { kind: 'product', name: 'Milk' } },
    ])('attributes a recorded total with recipe protein $total as $expected.kind', ({ total, expected }) => {
        const recipe = {
            ...createEmptyRecipeSnapshot(),
            id: entityId<'recipe'>('recipe-1'),
            name: 'Lemon water',
            servings: 2,
            totalProteins: total,
        };
        const recipeItem = mealItemFromStored({ id: entityId<'meal-item'>('recipe-item'), mealId: meal.id }, 1, { product: null, recipe });

        expect(buildNutritionSources([{ ...meal, items: [...meal.items, recipeItem] }], 'protein', 'en')).toMatchObject([
            { ...expected, value: savedProtein },
        ]);
    });
});
