import { describe, expect, it } from 'vitest';

import { calculateCalorieMismatchWarning, calculateCaloriesFromMacros, calculateMacroBarState } from './nutrition-form.utils';

const PROTEIN_10 = 10;
const PROTEIN_20 = 20;
const PROTEIN_25 = 25;
const PROTEIN_31 = 31;
const FAT_5 = 5;
const FAT_10 = 10;
const FAT_12 = 12;
const FAT_36 = 3.6;
const CARBS_7 = 7;
const CARBS_10 = 10;
const CARBS_20 = 20;
const CARBS_30 = 30;
const CARBS_50 = 50;
const CARBS_80 = 80;
const ALCOHOL_5 = 5;
const ALCOHOL_10 = 10;
const NEGATIVE_PROTEIN = -10;
const NEGATIVE_FAT = -5;
const NEGATIVE_CARBS = -20;
const NEGATIVE_ALCOHOL = -3;
const SMALL_NEGATIVE_PROTEIN = -5;
const SMALL_NEGATIVE_FAT = -3;
const SMALL_NEGATIVE_CARBS = -2;
const CALORIES_40 = 40;
const CALORIES_50 = 50;
const CALORIES_58 = 58;
const CALORIES_70 = 70;
const CALORIES_100 = 100;
const CALORIES_110 = 110;
const CALORIES_120 = 120;
const CALORIES_130 = 130;
const CALORIES_165 = 165;
const CALORIES_170 = 170;
const CALORIES_200 = 200;
const CALORIES_325 = 325;
const ONE_THIRD_PERCENT = 33.333;
const CHICKEN_PROTEIN_PERCENT = 89.595;
const CHICKEN_FAT_PERCENT = 10.405;
const FULL_PERCENT = 100;
const PRECISION_10 = 10;
const MACRO_SEGMENT_COUNT = 3;
const STRICT_THRESHOLD = 0.05;
const PERMISSIVE_THRESHOLD = 2.0;

describe('nutrition-form.utils', () => {
    registerCaloriesFromMacrosTests();
    registerCalorieMismatchTests();
    registerMacroBarStateTests();
});

function registerCaloriesFromMacrosTests(): void {
    describe('calculateCaloriesFromMacros', () => {
        it('should calculate P*4 + F*9 + C*4', () => {
            expect(calculateCaloriesFromMacros(PROTEIN_10, FAT_5, CARBS_20)).toBe(CALORIES_165);
        });

        it('should include alcohol at 7 kcal/g', () => {
            expect(calculateCaloriesFromMacros(0, 0, 0, ALCOHOL_10)).toBe(CALORIES_70);
        });

        it('should calculate mixed macros with alcohol', () => {
            expect(calculateCaloriesFromMacros(PROTEIN_20, FAT_10, CARBS_30, ALCOHOL_5)).toBe(CALORIES_325);
        });

        it('should treat null values as 0', () => {
            expect(calculateCaloriesFromMacros(null, null, null, null)).toBe(0);
        });

        it('should treat undefined values as 0', () => {
            expect(calculateCaloriesFromMacros(undefined, undefined, undefined, undefined)).toBe(0);
        });

        it('should treat NaN as 0', () => {
            expect(calculateCaloriesFromMacros(NaN, NaN, NaN, NaN)).toBe(0);
        });

        it('should treat Infinity as 0', () => {
            expect(calculateCaloriesFromMacros(Infinity, -Infinity, Infinity, -Infinity)).toBe(0);
        });

        it('should treat negative values as 0', () => {
            expect(calculateCaloriesFromMacros(NEGATIVE_PROTEIN, NEGATIVE_FAT, NEGATIVE_CARBS, NEGATIVE_ALCOHOL)).toBe(0);
        });

        it('should default alcohol to 0 when omitted', () => {
            expect(calculateCaloriesFromMacros(PROTEIN_10, FAT_5, CARBS_20)).toBe(CALORIES_165);
        });

        it('should handle a mix of valid and null values', () => {
            expect(calculateCaloriesFromMacros(PROTEIN_10, null, undefined)).toBe(CALORIES_40);
        });
    });
}

function registerCalorieMismatchTests(): void {
    describe('calculateCalorieMismatchWarning', () => {
        it('should return null when calories is 0', () => {
            expect(calculateCalorieMismatchWarning({ calories: 0, proteins: PROTEIN_10, fats: FAT_5, carbs: CARBS_20 })).toBeNull();
        });

        it('should return null when all macros are 0 (expectedCalories is 0)', () => {
            expect(calculateCalorieMismatchWarning({ calories: CALORIES_100, proteins: 0, fats: 0, carbs: 0 })).toBeNull();
        });

        it('should return null when deviation is within default threshold (20%)', () => {
            expect(
                calculateCalorieMismatchWarning({ calories: CALORIES_165, proteins: PROTEIN_10, fats: FAT_5, carbs: CARBS_20 }),
            ).toBeNull();
        });

        it('should return null when deviation is exactly at the threshold', () => {
            expect(calculateCalorieMismatchWarning({ calories: CALORIES_120, proteins: PROTEIN_25, fats: 0, carbs: 0 })).toBeNull();
        });

        it('should return warning when deviation exceeds threshold', () => {
            const result = calculateCalorieMismatchWarning({ calories: CALORIES_200, proteins: PROTEIN_25, fats: 0, carbs: 0 });
            expect(result).toEqual({
                expectedCalories: CALORIES_100,
                actualCalories: CALORIES_200,
            });
        });

        it('should return warning when actual is much lower than expected', () => {
            const result = calculateCalorieMismatchWarning({
                calories: CALORIES_50,
                proteins: PROTEIN_10,
                fats: FAT_10,
                carbs: CARBS_10,
            });
            expect(result).toEqual({
                expectedCalories: CALORIES_170,
                actualCalories: CALORIES_50,
            });
        });

        registerAdditionalCalorieMismatchTests();
    });
}

function registerAdditionalCalorieMismatchTests(): void {
    it('should include alcohol in expected calories', () => {
        const result = calculateCalorieMismatchWarning({ calories: CALORIES_100, proteins: 0, fats: 0, carbs: 0, alcohol: ALCOHOL_10 });
        expect(result).toEqual({
            expectedCalories: CALORIES_70,
            actualCalories: CALORIES_100,
        });
    });

    it('should respect custom threshold', () => {
        const result = calculateCalorieMismatchWarning({
            calories: CALORIES_110,
            proteins: PROTEIN_25,
            fats: 0,
            carbs: 0,
            alcohol: 0,
            threshold: STRICT_THRESHOLD,
        });
        expect(result).toEqual({
            expectedCalories: CALORIES_100,
            actualCalories: CALORIES_110,
        });
    });

    it('should not warn with a very permissive threshold', () => {
        expect(
            calculateCalorieMismatchWarning({
                calories: CALORIES_200,
                proteins: PROTEIN_25,
                fats: 0,
                carbs: 0,
                alcohol: 0,
                threshold: PERMISSIVE_THRESHOLD,
            }),
        ).toBeNull();
    });

    it('should round expected and actual calories', () => {
        const result = calculateCalorieMismatchWarning({ calories: CALORIES_100, proteins: 3, fats: 2, carbs: CARBS_7 });
        expect(result).toEqual({
            expectedCalories: CALORIES_58,
            actualCalories: CALORIES_100,
        });
    });

    it('should default alcohol to 0', () => {
        const result = calculateCalorieMismatchWarning({ calories: CALORIES_130, proteins: PROTEIN_25, fats: 0, carbs: 0 });
        expect(result).toEqual({
            expectedCalories: CALORIES_100,
            actualCalories: CALORIES_130,
        });
    });
}

function registerMacroBarStateTests(): void {
    describe('calculateMacroBarState', () => {
        it('should return empty state when all values are 0', () => {
            const result = calculateMacroBarState(0, 0, 0);
            expect(result).toEqual({ isEmpty: true, segments: [] });
        });

        it('should return empty state when all values are negative', () => {
            const result = calculateMacroBarState(SMALL_NEGATIVE_PROTEIN, SMALL_NEGATIVE_FAT, SMALL_NEGATIVE_CARBS);
            expect(result).toEqual({ isEmpty: true, segments: [] });
        });

        it('should calculate percentages for all three macros', () => {
            const result = calculateMacroBarState(PROTEIN_10, FAT_10, CARBS_10);
            expect(result.isEmpty).toBe(false);
            expect(result.segments.length).toBe(MACRO_SEGMENT_COUNT);
            expect(result.segments[0].key).toBe('proteins');
            expect(result.segments[0].percent).toBeCloseTo(ONE_THIRD_PERCENT, 2);
            expect(result.segments[1].key).toBe('fats');
            expect(result.segments[1].percent).toBeCloseTo(ONE_THIRD_PERCENT, 2);
            expect(result.segments[2].key).toBe('carbs');
            expect(result.segments[2].percent).toBeCloseTo(ONE_THIRD_PERCENT, 2);
        });

        registerAdditionalMacroBarStateTests();
    });
}

function registerAdditionalMacroBarStateTests(): void {
    it('should only include positive macros', () => {
        const result = calculateMacroBarState(PROTEIN_20, 0, CARBS_80);
        expect(result.isEmpty).toBe(false);
        expect(result.segments.length).toBe(2);
        expect(result.segments).toEqual([
            { key: 'proteins', percent: PROTEIN_20 },
            { key: 'carbs', percent: CARBS_80 },
        ]);
    });

    it('should handle single positive macro', () => {
        const result = calculateMacroBarState(0, FAT_10, 0);
        expect(result.isEmpty).toBe(false);
        expect(result.segments).toEqual([{ key: 'fats', percent: FULL_PERCENT }]);
    });

    it('should exclude negative values from segments', () => {
        const result = calculateMacroBarState(SMALL_NEGATIVE_PROTEIN, FAT_10, 0);
        expect(result.isEmpty).toBe(false);
        expect(result.segments).toEqual([{ key: 'fats', percent: FULL_PERCENT }]);
    });

    it('should calculate correct percentages for realistic macros', () => {
        const result = calculateMacroBarState(PROTEIN_31, FAT_36, 0);
        expect(result.isEmpty).toBe(false);
        expect(result.segments.length).toBe(2);

        const proteinSegment = result.segments.find(s => s.key === 'proteins');
        const fatSegment = result.segments.find(s => s.key === 'fats');

        expect(proteinSegment?.percent).toBeCloseTo(CHICKEN_PROTEIN_PERCENT, 2);
        expect(fatSegment?.percent).toBeCloseTo(CHICKEN_FAT_PERCENT, 2);
    });

    it('should have segments that sum to 100%', () => {
        const result = calculateMacroBarState(PROTEIN_25, FAT_12, CARBS_50);
        const totalPercent = result.segments.reduce((sum, s) => sum + s.percent, 0);
        expect(totalPercent).toBeCloseTo(FULL_PERCENT, PRECISION_10);
    });
}
