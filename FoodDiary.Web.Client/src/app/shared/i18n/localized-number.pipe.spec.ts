import { describe, expect, it } from 'vitest';

import { LocalizedNumberPipe } from './localized-number.pipe';

const VALUE = 2258;
const pipe = new LocalizedNumberPipe();

describe('LocalizedNumberPipe', () => {
    it('formats Russian thousands without a comma', () => {
        expect(pipe.transform(VALUE, 'ru')).not.toContain(',');
        expect(pipe.transform(VALUE, 'ru').replace(/\s/u, ' ')).toBe('2 258');
    });

    it('uses English grouping for English', () => {
        expect(pipe.transform(VALUE, 'en')).toBe('2,258');
    });

    it('returns an empty string for absent or non-finite values', () => {
        expect(pipe.transform(null, 'ru')).toBe('');
        expect(pipe.transform(Number.NaN, 'ru')).toBe('');
    });
});
