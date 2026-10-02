import { describe, expect, it } from 'vitest';

import type { CatalogRecipe } from '../models/catalog-transfer';
import { orderCatalogRecipes, parseCatalogFile } from './catalog-file';

const firstId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const secondId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

function recipe(id: string, nestedRecipeId: string | null = null): CatalogRecipe {
    return {
        id,
        name: 'Каша',
        description: null,
        category: 'breakfast',
        imageUrl: null,
        prepTime: null,
        cookTime: null,
        servings: 1,
        language: 'ru',
        languageConfirmed: true,
        calculateNutritionAutomatically: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        steps: [
            {
                order: 1,
                description: 'Варить',
                title: null,
                imageUrl: null,
                ingredients: nestedRecipeId !== null ? [{ productId: null, nestedRecipeId, amount: 1 }] : [],
            },
        ],
    };
}

function serialize(recipes: CatalogRecipe[]): string {
    return JSON.stringify({ format: 'fooddiary-catalog', version: 1, products: [], recipes });
}

describe('catalog transfer files', () => {
    it('preserves Russian text and normalizes GUID references', () => {
        const parsed = parseCatalogFile(serialize([recipe(firstId.toUpperCase(), secondId.toUpperCase())]));
        expect(parsed.recipes[0].name).toBe('Каша');
        expect(parsed.recipes[0].id).toBe(firstId);
        expect(parsed.recipes[0].steps[0].ingredients[0].nestedRecipeId).toBe(secondId);
    });

    it('rejects unsupported versions and malformed nested collections', () => {
        expect(() => parseCatalogFile('{"format":"fooddiary-catalog","version":2,"products":[],"recipes":[]}')).toThrow();
        const invalid = { ...recipe(firstId), steps: [null] };
        expect(() =>
            parseCatalogFile(JSON.stringify({ format: 'fooddiary-catalog', version: 1, products: [], recipes: [invalid] })),
        ).toThrow();
    });

    it('rejects case-insensitive duplicate IDs', () => {
        expect(() => parseCatalogFile(serialize([recipe(firstId), recipe(firstId.toUpperCase())]))).toThrow(
            'ADMIN_CATALOG_TRANSFER.DUPLICATE_IDS',
        );
    });

    it('imports nested recipes before their dependants', () => {
        expect(orderCatalogRecipes([recipe(firstId, secondId), recipe(secondId)], new Set()).map(item => item.id)).toEqual([
            secondId,
            firstId,
        ]);
    });

    it('accepts existing public dependencies and skips reordering existing recipes', () => {
        expect(orderCatalogRecipes([recipe(firstId, secondId)], new Set([secondId]))).toHaveLength(1);
        expect(orderCatalogRecipes([recipe(firstId, firstId)], new Set([firstId]))).toHaveLength(1);
    });

    it('rejects missing nested recipes and cycles', () => {
        expect(() => orderCatalogRecipes([recipe(firstId, secondId)], new Set())).toThrow('ADMIN_CATALOG_TRANSFER.RECIPE_LINKS');
        expect(() => orderCatalogRecipes([recipe(firstId, secondId), recipe(secondId, firstId)], new Set())).toThrow(
            'ADMIN_CATALOG_TRANSFER.RECIPE_LINKS',
        );
        expect(() => orderCatalogRecipes([recipe(firstId, firstId)], new Set())).toThrow('ADMIN_CATALOG_TRANSFER.RECIPE_LINKS');
    });
});
