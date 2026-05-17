import { describe, expect, it } from 'vitest';

import { parseDecimalInput, parseIntegerInput } from './number.utils';

const NUMBER_INPUT = 12;
const STRING_INPUT = 24;
const MIXED_STRING_INPUT = 36;
const DECIMAL_NUMBER_INPUT = 12.5;
const DECIMAL_STRING_INPUT = 24.75;
const COMMA_DECIMAL_INPUT = 36.5;

describe('parseIntegerInput', () => {
    it('returns integer values from number and string inputs', () => {
        expect(parseIntegerInput(NUMBER_INPUT)).toBe(NUMBER_INPUT);
        expect(parseIntegerInput(String(STRING_INPUT))).toBe(STRING_INPUT);
        expect(parseIntegerInput(`${MIXED_STRING_INPUT} hours`)).toBe(MIXED_STRING_INPUT);
    });

    it('returns null for invalid values', () => {
        expect(parseIntegerInput('abc')).toBeNull();
        expect(parseIntegerInput(Number.NaN)).toBeNull();
    });
});

describe('parseDecimalInput', () => {
    it('returns decimal values from number and string inputs', () => {
        expect(parseDecimalInput(DECIMAL_NUMBER_INPUT)).toBe(DECIMAL_NUMBER_INPUT);
        expect(parseDecimalInput('24.75')).toBe(DECIMAL_STRING_INPUT);
        expect(parseDecimalInput('36,5')).toBe(COMMA_DECIMAL_INPUT);
    });

    it('returns null for empty or invalid values', () => {
        expect(parseDecimalInput(null)).toBeNull();
        expect(parseDecimalInput(undefined)).toBeNull();
        expect(parseDecimalInput('abc')).toBeNull();
        expect(parseDecimalInput(Number.NaN)).toBeNull();
    });
});
