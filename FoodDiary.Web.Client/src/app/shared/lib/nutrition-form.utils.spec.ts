import { describe, expect, it } from 'vitest';
import { calculateCaloriesFromMacros, calculateCalorieMismatchWarning, calculateMacroBarState } from './nutrition-form.utils';

describe('nutrition-form.utils', () => {
    describe('calculateCaloriesFromMacros', () => {
        it('should calculate P*4 + F*9 + C*4', () => {
            // 10*4 + 5*9 + 20*4 = 40 + 45 + 80 = 165
            expect(calculateCaloriesFromMacros(10, 5, 20)).toBe(165);
        });

        it('should include alcohol at 7 kcal/g', () => {
            // 0 + 0 + 0 + 10*7 = 70
            expect(calculateCaloriesFromMacros(0, 0, 0, 10)).toBe(70);
        });

        it('should calculate mixed macros with alcohol', () => {
            // 20*4 + 10*9 + 30*4 + 5*7 = 80 + 90 + 120 + 35 = 325
            expect(calculateCaloriesFromMacros(20, 10, 30, 5)).toBe(325);
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
            expect(calculateCaloriesFromMacros(-10, -5, -20, -3)).toBe(0);
        });

        it('should default alcohol to 0 when omitted', () => {
            expect(calculateCaloriesFromMacros(10, 5, 20)).toBe(165);
        });

        it('should handle a mix of valid and null values', () => {
            // 10*4 + 0 + 0 = 40
            expect(calculateCaloriesFromMacros(10, null, undefined)).toBe(40);
        });
    });

    describe('calculateCalorieMismatchWarning', () => {
        it('should return null when calories is 0', () => {
            expect(calculateCalorieMismatchWarning(0, 10, 5, 20)).toBeNull();
        });

        it('should return null when all macros are 0 (expectedCalories is 0)', () => {
            expect(calculateCalorieMismatchWarning(100, 0, 0, 0)).toBeNull();
        });

        it('should return null when deviation is within default threshold (20%)', () => {
            // expected = 10*4 + 5*9 + 20*4 = 165
            // actual = 165 => deviation = 0
            expect(calculateCalorieMismatchWarning(165, 10, 5, 20)).toBeNull();
        });

        it('should return null when deviation is exactly at the threshold', () => {
            // expected = 100 (e.g. 25*4 + 0 + 0)
            // actual = 120 => deviation = 20/100 = 0.2
            expect(calculateCalorieMismatchWarning(120, 25, 0, 0)).toBeNull();
        });

        it('should return warning when deviation exceeds threshold', () => {
            // expected = 100, actual = 200 => deviation = 1.0
            const result = calculateCalorieMismatchWarning(200, 25, 0, 0);
            expect(result).toEqual({
                expectedCalories: 100,
                actualCalories: 200,
            });
        });

        it('should return warning when actual is much lower than expected', () => {
            // expected = 10*4 + 10*9 + 10*4 = 170
            // actual = 50 => deviation = 120/170 ~ 0.706
            const result = calculateCalorieMismatchWarning(50, 10, 10, 10);
            expect(result).toEqual({
                expectedCalories: 170,
                actualCalories: 50,
            });
        });

        it('should include alcohol in expected calories', () => {
            // expected = 0 + 0 + 0 + 10*7 = 70
            // actual = 100 => deviation = 30/70 ~ 0.43
            const result = calculateCalorieMismatchWarning(100, 0, 0, 0, 10);
            expect(result).toEqual({
                expectedCalories: 70,
                actualCalories: 100,
            });
        });

        it('should respect custom threshold', () => {
            // expected = 100, actual = 110 => deviation = 0.1
            // With threshold 0.05 this should warn
            const result = calculateCalorieMismatchWarning(110, 25, 0, 0, 0, 0.05);
            expect(result).toEqual({
                expectedCalories: 100,
                actualCalories: 110,
            });
        });

        it('should not warn with a very permissive threshold', () => {
            // expected = 100, actual = 200 => deviation = 1.0
            // With threshold 2.0 this should not warn
            expect(calculateCalorieMismatchWarning(200, 25, 0, 0, 0, 2.0)).toBeNull();
        });

        it('should round expected and actual calories', () => {
            // expected = 3*4 + 2*9 + 7*4 = 12 + 18 + 28 = 58
            // actual = 100 => deviation > 0.2
            const result = calculateCalorieMismatchWarning(100, 3, 2, 7);
            expect(result).toEqual({
                expectedCalories: 58,
                actualCalories: 100,
            });
        });

        it('should default alcohol to 0', () => {
            // expected = 25*4 = 100, actual = 130 => deviation = 0.3
            const result = calculateCalorieMismatchWarning(130, 25, 0, 0);
            expect(result).toEqual({
                expectedCalories: 100,
                actualCalories: 130,
            });
        });
    });

    describe('calculateMacroBarState', () => {
        it('should return empty state when all values are 0', () => {
            const result = calculateMacroBarState(0, 0, 0);
            expect(result).toEqual({ isEmpty: true, segments: [] });
        });

        it('should return empty state when all values are negative', () => {
            const result = calculateMacroBarState(-5, -3, -2);
            expect(result).toEqual({ isEmpty: true, segments: [] });
        });

        it('should calculate percentages for all three macros', () => {
            // total = 10 + 10 + 10 = 30
            const result = calculateMacroBarState(10, 10, 10);
            expect(result.isEmpty).toBe(false);
            expect(result.segments.length).toBe(3);
            expect(result.segments[0].key).toBe('proteins');
            expect(result.segments[0].percent).toBeCloseTo(33.333, 2);
            expect(result.segments[1].key).toBe('fats');
            expect(result.segments[1].percent).toBeCloseTo(33.333, 2);
            expect(result.segments[2].key).toBe('carbs');
            expect(result.segments[2].percent).toBeCloseTo(33.333, 2);
        });

        it('should only include positive macros', () => {
            const result = calculateMacroBarState(20, 0, 80);
            expect(result.isEmpty).toBe(false);
            expect(result.segments.length).toBe(2);
            expect(result.segments).toEqual([
                { key: 'proteins', percent: 20 },
                { key: 'carbs', percent: 80 },
            ]);
        });

        it('should handle single positive macro', () => {
            const result = calculateMacroBarState(0, 10, 0);
            expect(result.isEmpty).toBe(false);
            expect(result.segments).toEqual([{ key: 'fats', percent: 100 }]);
        });

        it('should exclude negative values from segments', () => {
            const result = calculateMacroBarState(-5, 10, 0);
            expect(result.isEmpty).toBe(false);
            expect(result.segments).toEqual([{ key: 'fats', percent: 100 }]);
        });

        it('should calculate correct percentages for realistic macros', () => {
            // Chicken breast: 31g protein, 3.6g fat, 0g carbs
            const result = calculateMacroBarState(31, 3.6, 0);
            expect(result.isEmpty).toBe(false);
            expect(result.segments.length).toBe(2);

            const proteinSegment = result.segments.find(s => s.key === 'proteins')!;
            const fatSegment = result.segments.find(s => s.key === 'fats')!;

            expect(proteinSegment.percent).toBeCloseTo(89.595, 2);
            expect(fatSegment.percent).toBeCloseTo(10.405, 2);
        });

        it('should have segments that sum to 100%', () => {
            const result = calculateMacroBarState(25, 12, 50);
            const totalPercent = result.segments.reduce((sum, s) => sum + s.percent, 0);
            expect(totalPercent).toBeCloseTo(100, 10);
        });
    });
});
