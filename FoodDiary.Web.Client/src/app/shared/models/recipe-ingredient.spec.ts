import { describe, expect, it } from 'vitest';

import { ingredientEditorSource, recipeIngredientFromStored } from './recipe-ingredient';

const LARGE_INGREDIENT_AMOUNT = 2_000_000;
const FRACTIONAL_SERVINGS = 1.25;

describe('recipe ingredient source observations', () => {
    it('decodes product quantities without imposing meal consumption bounds', () => {
        const ingredient = recipeIngredientFromStored({ id: 'i', productId: 'p', amount: LARGE_INGREDIENT_AMOUNT, productBaseUnit: 'ML' });
        expect(ingredient).toMatchObject({ kind: 'product', productId: 'p', amount: LARGE_INGREDIENT_AMOUNT, productBaseUnit: 'ML' });
        expect(ingredientEditorSource(ingredient)).toBe('product');
    });

    it('retains fractional nested servings and incomplete nutrition snapshots', () => {
        const fields = {
            id: 'i',
            nestedRecipeId: 'r',
            amount: FRACTIONAL_SERVINGS,
            nestedRecipeTotalFiber: null,
            nestedRecipeTotalAlcohol: 0,
        };
        const ingredient = recipeIngredientFromStored(fields);
        expect(ingredient).toMatchObject({ ...fields, kind: 'recipe' });
        expect(ingredientEditorSource(ingredient)).toBe('recipe');
    });

    it('preserves blank text and an observed amount without inventing a numeric text quantity', () => {
        const fields = { id: 'i', textName: '', amountText: 'a pinch', amount: 0 };
        expect(recipeIngredientFromStored(fields)).toEqual({ ...fields, kind: 'text', productId: undefined, nestedRecipeId: undefined });
    });

    it.each([
        { productId: 'p', nestedRecipeId: 'r', textName: 'text', expected: 'text' },
        { productId: 'p', nestedRecipeId: 'r', expected: 'recipe' },
        { productId: '', nestedRecipeId: null, expected: 'missing' },
        { productName: 'deleted product', expected: 'missing' },
    ])('retains historical sources and editor precedence for $expected', ({ expected, ...sources }) => {
        const fields = { id: 'i', amount: FRACTIONAL_SERVINGS, productBaseUnit: 'G', productFiberPerBase: 0, ...sources };
        const ingredient = recipeIngredientFromStored(fields);
        expect(ingredient.kind).toBe('legacy');
        expect(ingredient).toMatchObject(fields);
        expect(ingredientEditorSource(ingredient)).toBe(expected);
    });

    it.each([0, -1, Number.NaN, Number.POSITIVE_INFINITY])('retains stored numeric observation %s', amount => {
        expect(recipeIngredientFromStored({ id: 'i', productId: 'p', amount }).amount).toBe(amount);
    });
});
