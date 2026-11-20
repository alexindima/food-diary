import { describe, expect, it } from 'vitest';

import type { PublicRecipeIngredient } from '../models/public-recipe.data';
import { publicRecipeFixture } from './public-recipe.test-helper';
import { publicRecipeSeo, recipeImages, scaleIngredient } from './public-recipe.utils';

const TARGET_SERVINGS = 4;
const EXPECTED_AMOUNT = 200;
const SCHEMA_CARROT_GRAMS = 100;

describe('public recipe presentation', () => {
    it('preserves cover order without duplicate photos', () => {
        expect(recipeImages(publicRecipeFixture())).toEqual(['cover.jpg', 'second.jpg']);
        expect(recipeImages({ imageUrl: null, images: [] })).toEqual([]);
    });
    it('scales numeric quantities, leaving free text unscaled', () => {
        const ingredient: PublicRecipeIngredient = {
            name: 'Rice',
            amount: 100,
            unit: 'g',
            amountText: null,
            recipeId: null,
            isAvailable: true,
        };
        expect(scaleIngredient(ingredient, TARGET_SERVINGS, 2)).toBe(EXPECTED_AMOUNT);
        expect(scaleIngredient({ ...ingredient, amount: null, amountText: 'to taste' }, TARGET_SERVINGS, 2)).toBeNull();
    });
    it('omits unavailable ingredients and incomplete nutrition from structured data', () => {
        const recipe = publicRecipeFixture();
        recipe.steps[0].ingredients = [{ name: null, amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false }];
        const seo = publicRecipeSeo(recipe);
        expect(seo.title).toBe('Soup');
        expect(seo.recipeStructuredData?.['recipeIngredient']).toEqual([]);
        expect(seo.recipeStructuredData).not.toHaveProperty('nutrition');
    });
});

describe('public recipe search metadata', () => {
    it('uses the first instruction as description and omits absent schema fields', () => {
        const recipe = {
            ...publicRecipeFixture(),
            description: null,
            category: null,
            prepTime: null,
            cookTime: null,
        };

        const seo = publicRecipeSeo(recipe);

        expect(seo.description).toBe('Boil');
        const serialized = JSON.stringify(seo.recipeStructuredData);
        expect(serialized).not.toMatch(/"(?:description|recipeCategory|prepTime|cookTime)":/u);
        expect(seo.recipeStructuredData?.['recipeInstructions']).toEqual([{ '@type': 'HowToStep', text: 'Boil', image: [] }]);
    });

    it('uses the recipe title when a legacy public recipe has no instructions or description', () => {
        const recipe = { ...publicRecipeFixture(), description: null, imageUrl: null, images: [], steps: [] };

        const seo = publicRecipeSeo(recipe);

        expect(seo.title).toBe(recipe.name);
        expect(seo.description).toBe(recipe.name);
        expect(seo.imageUrl).toBeUndefined();
        expect(seo.recipeStructuredData?.['image']).toEqual([]);
        expect(seo.recipeStructuredData?.['recipeInstructions']).toEqual([]);
    });

    it('preserves free-text amounts, numeric units and named steps when publishing recipe schema', () => {
        const recipe = publicRecipeFixture();
        recipe.steps[0] = {
            ...recipe.steps[0],
            title: 'Prepare the vegetables',
            images: ['prep.jpg'],
            ingredients: [
                { name: 'Black pepper', amount: null, unit: null, amountText: 'to taste', recipeId: null, isAvailable: true },
                { name: 'Carrot', amount: SCHEMA_CARROT_GRAMS, unit: 'g', amountText: null, recipeId: null, isAvailable: true },
                { name: 'Unavailable', amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false },
            ],
        };

        const schema = publicRecipeSeo(recipe).recipeStructuredData;

        expect(schema?.['recipeIngredient']).toEqual(['Black pepper — to taste', 'Carrot — 100 g']);
        expect(schema?.['recipeInstructions']).toEqual([
            { '@type': 'HowToStep', name: 'Prepare the vegetables', text: 'Boil', image: ['prep.jpg'] },
        ]);
    });
});
