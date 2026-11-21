import { describe, expect, it } from 'vitest';

import type { TdeeInsight } from '../../../shared/models/tdee-insight.data';
import {
    buildTdeeConfidenceKey,
    buildTdeeHintKey,
    getEffectiveTdee,
    getTdeeWeightTrendKg,
    hasMeaningfulTdeeSuggestion,
} from './tdee-insight-view.mapper';

const insightBase: TdeeInsight = {
    estimatedTdee: 2100,
    adaptiveTdee: null,
    bmr: 1600,
    confidence: 'medium',
    dataDaysUsed: 20,
    weightTrendPerWeek: -0.25,
    currentCalorieTarget: 2000,
    suggestedCalorieTarget: 1900,
    goalAdjustmentHint: 'hint.reduce',
};
const ADAPTIVE_TDEE = 2050;
const ESTIMATED_TDEE = 2100;
const POSITIVE_WEIGHT_TREND = 0.125;
const NEGATIVE_WEIGHT_TREND = -0.125;
const ROUNDED_WEIGHT_TREND = 0.13;

describe('tdee insight view mapper', () => {
    it('prefers adaptive tdee over estimated value', () => {
        expect(getEffectiveTdee({ ...insightBase, adaptiveTdee: ADAPTIVE_TDEE })).toBe(ADAPTIVE_TDEE);
        expect(getEffectiveTdee(insightBase)).toBe(ESTIMATED_TDEE);
        expect(getEffectiveTdee(null)).toBeNull();
    });

    it('rounds weekly weight trend to two decimal places and keeps its sign and zero', () => {
        expect(getTdeeWeightTrendKg(POSITIVE_WEIGHT_TREND)).toBe(ROUNDED_WEIGHT_TREND);
        expect(getTdeeWeightTrendKg(NEGATIVE_WEIGHT_TREND)).toBe(-ROUNDED_WEIGHT_TREND);
        expect(getTdeeWeightTrendKg(0)).toBe(0);
    });

    it.each([null, undefined, Number.NaN, Number.POSITIVE_INFINITY])('omits an unavailable weekly trend (%s)', trend => {
        expect(getTdeeWeightTrendKg(trend)).toBeNull();
    });

    it('builds translation keys for hints and confidence', () => {
        expect(buildTdeeHintKey('hint.reduce')).toBe('TDEE_CARD.HINTS.REDUCE');
        expect(buildTdeeHintKey('')).toBeNull();
        expect(buildTdeeConfidenceKey(insightBase, 'EMPTY')).toBe('TDEE_CARD.CONFIDENCE.MEDIUM');
        expect(buildTdeeConfidenceKey({ ...insightBase, confidence: 'none' }, 'EMPTY')).toBe('EMPTY');
    });

    it('requires meaningful positive calorie targets for suggestion', () => {
        expect(hasMeaningfulTdeeSuggestion(insightBase)).toBe(true);
        expect(hasMeaningfulTdeeSuggestion({ ...insightBase, suggestedCalorieTarget: 2030 })).toBe(false);
        expect(hasMeaningfulTdeeSuggestion({ ...insightBase, suggestedCalorieTarget: null })).toBe(false);
    });
});
