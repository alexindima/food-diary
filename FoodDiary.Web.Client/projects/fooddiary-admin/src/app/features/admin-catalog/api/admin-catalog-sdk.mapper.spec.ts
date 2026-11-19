import { describe, expect, it } from 'vitest';

import { catalogProductFromSdk, catalogRecipeExportFromSdk, catalogRecipeImportFromSdk } from './admin-catalog-sdk.mapper';

const manualProtein = 1.23456789;
const ingredientAmount = 0.125;

describe('Admin catalog wire mapping', () => {
    it('keeps the base serving and normalizes optional product fields across exports', () => {
        const product = {
            id: 'water',
            name: 'Water',
            productType: 'beverage',
            baseUnit: 'ml',
            baseAmount: 100,
            defaultPortionAmount: 250,
            caloriesPerBase: 0,
            proteinsPerBase: 0,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
        };
        const details = {
            barcode: '0123456789',
            brand: 'Spring',
            category: 'Drinks',
            description: 'Still water',
            imageUrl: 'https://images.example.test/water.png',
        };

        expect(catalogProductFromSdk(product)).toEqual({
            ...product,
            barcode: null,
            brand: null,
            category: null,
            description: null,
            imageUrl: null,
        });
        expect(catalogProductFromSdk({ ...product, ...details })).toEqual({ ...product, ...details });
    });

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

    it('keeps recipe import errors associated with the corresponding item', () => {
        expect(catalogRecipeImportFromSdk({ id: 'recipe', status: 'invalid', errors: ['Ingredient is missing'] })).toEqual({
            id: 'recipe',
            status: 'invalid',
            errors: ['Ingredient is missing'],
        });
    });
});
