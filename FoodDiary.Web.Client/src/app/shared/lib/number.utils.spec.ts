import { describe, expect, it } from 'vitest';

import { parseIntegerInput } from './number.utils';

describe('parseIntegerInput', () => {
    it('returns integer values from number and string inputs', () => {
        expect(parseIntegerInput(12)).toBe(12);
        expect(parseIntegerInput('24')).toBe(24);
        expect(parseIntegerInput('36 hours')).toBe(36);
    });

    it('returns null for invalid values', () => {
        expect(parseIntegerInput('abc')).toBeNull();
        expect(parseIntegerInput(Number.NaN)).toBeNull();
    });
});
