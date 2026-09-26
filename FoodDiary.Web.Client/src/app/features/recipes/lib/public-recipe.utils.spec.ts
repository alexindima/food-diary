import { describe, expect, it } from 'vitest';

import type { PublicRecipeIngredient } from '../models/public-recipe.data';
import { publicRecipeFixture } from './public-recipe.test-helper';
import { publicRecipeSeo, recipeImages, scaleIngredient } from './public-recipe.utils';

const TARGET_SERVINGS = 4;
const EXPECTED_AMOUNT = 200;

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
