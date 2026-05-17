import { describe, expect, it } from 'vitest';

import { resolveMealImageUrl } from './meal-image.util';

describe('meal image utils', () => {
    it('returns explicit non-empty image url', () => {
        expect(resolveMealImageUrl('https://example.com/meal.jpg', 'breakfast')).toBe('https://example.com/meal.jpg');
    });

    it('resolves known meal type stubs case-insensitively', () => {
        expect(resolveMealImageUrl(null, 'breakfast')).toBe('assets/images/stubs/meals/breakfast.svg');
        expect(resolveMealImageUrl(undefined, 'DINNER')).toBe('assets/images/stubs/meals/dinner.svg');
    });

    it('falls back to other stub for blank or unknown meal type', () => {
        expect(resolveMealImageUrl('', '')).toBe('assets/images/stubs/meals/other.svg');
        expect(resolveMealImageUrl('   ', 'brunch')).toBe('assets/images/stubs/meals/other.svg');
    });
});
