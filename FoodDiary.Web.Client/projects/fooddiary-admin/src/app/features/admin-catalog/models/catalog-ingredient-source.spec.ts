import { describe, expect, it } from 'vitest';

import { catalogProductReference, catalogRecipeReference, decodeCatalogIngredientSource } from './catalog-ingredient-source';

const PRODUCT_AMOUNT = 125.5;
const RECIPE_SERVINGS = 1.25;

describe('decoded catalog ingredient sources', () => {
    it('keeps product quantities separate from recipe servings', () => {
        const product = decodeCatalogIngredientSource({ productId: 'product', nestedRecipeId: null, amount: PRODUCT_AMOUNT });
        const recipe = decodeCatalogIngredientSource({ productId: null, nestedRecipeId: 'recipe', amount: RECIPE_SERVINGS });
        expect(product.kind).toBe('product');
        expect(recipe.kind).toBe('recipe');
        expect(product.amount).toBe(PRODUCT_AMOUNT);
        expect(recipe.amount).toBe(RECIPE_SERVINGS);
    });

    it('retains both historical references and permissive import amounts', () => {
        const source = decodeCatalogIngredientSource({ productId: 'product', nestedRecipeId: 'recipe', amount: -1 });
        expect(source.kind).toBe('legacy-dual');
        expect(catalogProductReference(source)).toBe('product');
        expect(catalogRecipeReference(source)).toBe('recipe');
        expect(source.amount).toBe(-1);
    });

    it('keeps text-only ingredients without inventing references', () => {
        const source = decodeCatalogIngredientSource({
            productId: null,
            nestedRecipeId: null,
            amount: 0,
            textName: 'Соль',
            amountText: 'по вкусу',
        });
        expect(source).toEqual({ kind: 'text', textName: 'Соль', amountText: 'по вкусу', amount: 0 });
        expect(catalogProductReference(source)).toBeNull();
        expect(catalogRecipeReference(source)).toBeNull();
    });
});
