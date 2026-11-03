import { describe, expect, it, vi } from 'vitest';

import { LocalizedNumberPipe } from './localized-number.pipe';

const NEGATIVE_DECIMAL = -1.18;
const VALUE = 2258;
const FIRST_DECIMAL = 1100.25;
const SECOND_DECIMAL = 2200.5;
const PRECISION_VALUE = 1234.5;
const PADDED_VALUE = 5678.9;
const FIXED_PRECISION = 3;
const DISTINCT_SETTINGS = 3;
const EVICTED_PRECISION = 4;
const MAX_VARIANT_PRECISION = 8;
const VALIDATION_VALUE = 12;
const pipe = new LocalizedNumberPipe();

describe('LocalizedNumberPipe', () => {
    it('formats Russian thousands without a comma', () => {
        expect(pipe.transform(VALUE, 'ru')).not.toContain(',');
        expect(pipe.transform(VALUE, 'ru').replace(/\s/u, ' ')).toBe('2 258');
    });

    it('uses English grouping for English', () => {
        expect(pipe.transform(VALUE, 'en')).toBe('2,258');
    });

    it('formats a negative decimal with a typographic minus', () => {
        expect(pipe.transform(NEGATIVE_DECIMAL, 'ru', 0, 2)).toBe('−1,18');
    });

    it('returns an empty string for absent or non-finite values', () => {
        expect(pipe.transform(null, 'ru')).toBe('');
        expect(pipe.transform(Number.NaN, 'ru')).toBe('');
    });

    it('reuses the formatter across values and pipe instances with the same locale and precision', () => {
        const constructor = vi.spyOn(Intl, 'NumberFormat');
        try {
            expect(new LocalizedNumberPipe().transform(FIRST_DECIMAL, 'en', 1, 2)).toBe('1,100.25');
            expect(new LocalizedNumberPipe().transform(SECOND_DECIMAL, 'en-US', 1, 2)).toBe('2,200.5');
            expect(constructor, 'identical locale and precision should create only one formatter').toHaveBeenCalledTimes(1);
        } finally {
            constructor.mockRestore();
        }
    });

    it('keeps language and fraction settings independent when reusing formatters', () => {
        const constructor = vi.spyOn(Intl, 'NumberFormat');
        try {
            expect(pipe.transform(PRECISION_VALUE, 'en', FIXED_PRECISION, FIXED_PRECISION)).toBe('1,234.500');
            expect(pipe.transform(PADDED_VALUE, 'en', FIXED_PRECISION, FIXED_PRECISION)).toBe('5,678.900');
            expect(pipe.transform(PRECISION_VALUE, 'ru', FIXED_PRECISION, FIXED_PRECISION).replaceAll(/\s/gu, ' ')).toBe('1 234,500');
            expect(pipe.transform(PRECISION_VALUE, 'en', 1, 1)).toBe('1,234.5');
            expect(constructor, 'only distinct locale and precision settings need new formatters').toHaveBeenCalledTimes(DISTINCT_SETTINGS);
        } finally {
            constructor.mockRestore();
        }
    });

    it('bounds retained formatters while preserving output after eviction', () => {
        const constructor = vi.spyOn(Intl, 'NumberFormat');
        try {
            expect(pipe.transform(1, 'en', EVICTED_PRECISION, EVICTED_PRECISION)).toBe('1.0000');
            for (let maximum = 0; maximum <= MAX_VARIANT_PRECISION; maximum++) {
                for (let minimum = 0; minimum <= maximum; minimum++) {
                    for (const language of ['en', 'ru']) {
                        pipe.transform(PRECISION_VALUE, language, minimum, maximum);
                    }
                }
            }
            constructor.mockClear();
            expect(pipe.transform(VALUE, 'en', EVICTED_PRECISION, EVICTED_PRECISION)).toBe('2,258.0000');
            expect(constructor).toHaveBeenCalledTimes(1);
        } finally {
            constructor.mockRestore();
        }
    });

    it('preserves native precision errors and skips formatters for invalid values', () => {
        expect(() => pipe.transform(VALIDATION_VALUE, 'en', 2, 1)).toThrow(RangeError);
        expect(() => pipe.transform(VALIDATION_VALUE, 'ru', -1, 2)).toThrow(RangeError);
        const constructor = vi.spyOn(Intl, 'NumberFormat');
        try {
            expect(pipe.transform(Number.POSITIVE_INFINITY, 'ru', 2, 1)).toBe('');
            expect(pipe.transform(undefined, 'en')).toBe('');
            expect(constructor).not.toHaveBeenCalled();
        } finally {
            constructor.mockRestore();
        }
    });
});
