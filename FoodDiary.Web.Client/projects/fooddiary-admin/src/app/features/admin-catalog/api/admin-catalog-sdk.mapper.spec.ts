import { describe, expect, it } from 'vitest';

import { catalogRecipeExportFromSdk } from './admin-catalog-sdk.mapper';

const manualProtein = 1.23456789;
const ingredientAmount = 0.125;

describe('Admin catalog wire mapping', () => {
    it('preserves manual zero, precision, nullability and nested ingredient quantities', () => {
        const recipe = catalogRecipeExportFromSdk({
            id: 'recipe',
            name: 'Recipe',
            servings: 3,
            language: 'ru',
            languageConfirmed: false,
            calculateNutritionAutomatically: false,
            manualCalories: 0,
            manualProteins: manualProtein,
            manualFats: null,
            steps: [
                {
                    order: 1,
                    description: 'Mix',
                    ingredients: [{ productId: 'product', nestedRecipeId: null, amount: ingredientAmount, amountText: null }],
                },
            ],
        });
        expect(recipe.manualCalories).toBe(0);
        expect(recipe.manualProteins).toBe(manualProtein);
        expect(recipe.manualFats).toBeNull();
        expect(recipe.calculateNutritionAutomatically).toBe(false);
        expect(recipe.steps[0].ingredients[0].amount).toBe(ingredientAmount);
        expect(recipe.steps[0].ingredients[0].nestedRecipeId).toBeNull();
        expect(recipe.steps[0].ingredients[0].amountText).toBeNull();
    });
});
