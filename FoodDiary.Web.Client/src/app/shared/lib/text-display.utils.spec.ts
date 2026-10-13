import { describe, expect, it } from 'vitest';

import { capitalizeFirstLetter } from './text-display.utils';

describe('capitalizeFirstLetter', () => {
    it.each([
        ['olive oil (or other oil)', 'Olive oil (or other oil)'],
        ['чёрный перец', 'Чёрный перец'],
        ['plain flour (Note 2 for GF)', 'Plain flour (Note 2 for GF)'],
        ['2% greek yogurt', '2% Greek yogurt'],
        ['  «оливковое масло»', '  «Оливковое масло»'],
        ['B12', 'B12'],
        ['100%', '100%'],
        ['', ''],
        [null, ''],
        [undefined, ''],
    ])('formats %s without rewriting the rest of the name', (name, expected) => {
        expect(capitalizeFirstLetter(name)).toBe(expected);
    });
});
