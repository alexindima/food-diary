import { describe, expect, it } from 'vitest';

import { parseIntegerInput } from './number.utils';

const NUMBER_INPUT = 12;
const STRING_INPUT = 24;
const MIXED_STRING_INPUT = 36;

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
