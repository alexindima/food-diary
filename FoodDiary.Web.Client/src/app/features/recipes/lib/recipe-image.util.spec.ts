import { describe, expect, it } from 'vitest';

import { resolveRecipeImageUrl } from './recipe-image.util';

const STUB_IMAGE_URL = 'assets/images/stubs/receipt.png';

describe('resolveRecipeImageUrl', () => {
    it('returns provided image URL when it has visible characters', () => {
        expect(resolveRecipeImageUrl('https://example.com/recipe.png')).toBe('https://example.com/recipe.png');
    });

    it('returns stub image URL for blank or missing values', () => {
        expect(resolveRecipeImageUrl(null)).toBe(STUB_IMAGE_URL);
        expect(resolveRecipeImageUrl(undefined)).toBe(STUB_IMAGE_URL);
        expect(resolveRecipeImageUrl('   ')).toBe(STUB_IMAGE_URL);
    });
});
