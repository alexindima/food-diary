import { describe, expect, it } from 'vitest';

import { isRecipeCategory, RECIPE_CATEGORIES, recipeCategoryKey } from './recipe-category';

describe('recipe category codes', () => {
    it('accepts all stable codes and derives localized labels', () => {
        for (const code of RECIPE_CATEGORIES) {
            expect(isRecipeCategory(code)).toBe(true);
            expect(recipeCategoryKey(code)).toBe(`RECIPE_CATEGORIES.${code}`);
        }
    });

    it.each([null, undefined, '', 'Салаты', 'Dinner', 'SALADS'])('rejects legacy or missing value %s and falls back to Other', value => {
        expect(isRecipeCategory(value)).toBe(false);
        expect(recipeCategoryKey(value)).toBe('RECIPE_CATEGORIES.other');
    });
});
