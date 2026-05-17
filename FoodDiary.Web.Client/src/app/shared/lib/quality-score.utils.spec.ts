import { describe, expect, it } from 'vitest';

import { DEFAULT_QUALITY_SCORE, normalizeQualityScore, QUALITY_SCORE_MAX, QUALITY_SCORE_MIN } from './quality-score.utils';

const FALLBACK_SCORE = 80;
const BELOW_MIN_SCORE = -10;
const ABOVE_MAX_SCORE = 120;
const FRACTIONAL_SCORE = 74.6;
const ROUNDED_SCORE = 75;

describe('quality score utils', () => {
    it('normalizes missing values to fallback score', () => {
        expect(normalizeQualityScore(null)).toBe(DEFAULT_QUALITY_SCORE);
        expect(normalizeQualityScore(undefined, FALLBACK_SCORE)).toBe(FALLBACK_SCORE);
    });

    it('clamps scores into allowed bounds', () => {
        expect(normalizeQualityScore(BELOW_MIN_SCORE)).toBe(QUALITY_SCORE_MIN);
        expect(normalizeQualityScore(ABOVE_MAX_SCORE)).toBe(QUALITY_SCORE_MAX);
    });

    it('rounds fractional scores', () => {
        expect(normalizeQualityScore(FRACTIONAL_SCORE)).toBe(ROUNDED_SCORE);
    });
});
