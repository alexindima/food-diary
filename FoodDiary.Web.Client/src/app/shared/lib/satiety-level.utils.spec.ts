import { describe, expect, it } from 'vitest';

import { MAX_SATIETY_LEVEL, MIN_SATIETY_LEVEL, normalizeSatietyLevel } from './satiety-level.utils';

const CURRENT_SCALE_VALUE = 4;
const LEGACY_SCALE_VALUE = 8;
const LEGACY_SCALE_RESULT = 4;
const HIGH_LEGACY_SCALE_VALUE = 20;

describe('satiety level utils', () => {
    it('preserves unanswered ratings instead of inventing a middle score', () => {
        expect(normalizeSatietyLevel(null)).toBeNull();
        expect(normalizeSatietyLevel(void 0)).toBeNull();
        expect(normalizeSatietyLevel(Number.NaN)).toBeNull();
        expect(normalizeSatietyLevel(0)).toBeNull();
    });

    it('keeps current scale values inside allowed range', () => {
        expect(normalizeSatietyLevel(1)).toBe(MIN_SATIETY_LEVEL);
        expect(normalizeSatietyLevel(CURRENT_SCALE_VALUE)).toBe(CURRENT_SCALE_VALUE);
    });

    it('converts legacy ten-point scale values to five-point scale', () => {
        expect(normalizeSatietyLevel(LEGACY_SCALE_VALUE)).toBe(LEGACY_SCALE_RESULT);
        expect(normalizeSatietyLevel(HIGH_LEGACY_SCALE_VALUE)).toBe(MAX_SATIETY_LEVEL);
    });
});
