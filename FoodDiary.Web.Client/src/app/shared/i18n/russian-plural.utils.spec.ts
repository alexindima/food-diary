import { describe, expect, it } from 'vitest';

import { resolveRussianPluralCategory } from './russian-plural.utils';

/* eslint-disable @typescript-eslint/no-magic-numbers -- These values are the boundary cases of the Russian pluralization rule. */
describe('resolveRussianPluralCategory', () => {
    it.each([
        [0, 'many'],
        [1, 'one'],
        [2, 'few'],
        [4, 'few'],
        [5, 'many'],
        [11, 'many'],
        [14, 'many'],
        [21, 'one'],
        [22, 'few'],
        [25, 'many'],
        [-1, 'one'],
    ] as const)('returns the expected category for %i', (count, category) => {
        expect(resolveRussianPluralCategory(count)).toBe(category);
    });
});
/* eslint-enable @typescript-eslint/no-magic-numbers -- Re-enable the rule after the boundary table. */
