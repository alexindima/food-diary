import { describe, expect, it } from 'vitest';

import { resolveRecipeImageUrl } from './recipe-image.util';

describe('resolveRecipeImageUrl', () => {
    it('returns provided image URL when it has visible characters', () => {
        expect(resolveRecipeImageUrl('https://example.com/recipe.png')).toBe('https://example.com/recipe.png');
    });

    it('lets the card render its theme-aware icon for blank or missing values', () => {
        expect(resolveRecipeImageUrl(null)).toBeUndefined();
        expect(resolveRecipeImageUrl(void 0)).toBeUndefined();
        expect(resolveRecipeImageUrl('   ')).toBeUndefined();
    });
});
