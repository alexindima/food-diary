import { describe, expect, it } from 'vitest';

import {
    planDayNumber,
    planDayNumberFromStored,
    planDurationDays,
    planDurationDaysFromStored,
    plannedMealTypeFromStored,
    plannedServings,
    plannedServingsFromStored,
} from './meal-plan-values';

const MAXIMUM_PLAN_DAYS = 31;
const DAY_OUTSIDE_PLAN_RANGE = MAXIMUM_PLAN_DAYS + 1;
const FRACTIONAL_PLAN_COUNT = 1.5;
const FRACTIONAL_CONSUMPTION = 0.5;
const ABOVE_CONSUMPTION_CAP = 1_000_001;
const MAXIMUM_BACKEND_COUNT = 2_147_483_647;

describe('meal plan quantities', () => {
    it.each([1, MAXIMUM_PLAN_DAYS])('retains inclusive day boundaries at %s', value => {
        expect(planDurationDays(value)).toBe(value);
        expect(planDayNumber(value)).toBe(value);
    });

    it.each([0, -1, FRACTIONAL_PLAN_COUNT, DAY_OUTSIDE_PLAN_RANGE, Number.NaN, Number.POSITIVE_INFINITY])(
        'rejects invalid duration/day %s',
        value => {
            expect(() => planDurationDays(value)).toThrow(RangeError);
            expect(() => planDayNumber(value)).toThrow(RangeError);
        },
    );

    it.each([1, 2, ABOVE_CONSUMPTION_CAP, MAXIMUM_BACKEND_COUNT])(
        'accepts positive integral planned servings %s without the consumption cap',
        value => {
            expect(plannedServings(value)).toBe(value);
        },
    );

    it.each([0, -1, FRACTIONAL_CONSUMPTION, Number.NaN, Number.POSITIVE_INFINITY])('rejects invalid planned servings %s', value => {
        expect(() => plannedServings(value)).toThrow(RangeError);
    });

    it('preserves legacy read projections and unknown meal type codes', () => {
        expect(planDurationDaysFromStored(0)).toBe(0);
        expect(planDayNumberFromStored(-1)).toBe(-1);
        expect(plannedServingsFromStored(FRACTIONAL_PLAN_COUNT)).toBe(FRACTIONAL_PLAN_COUNT);
        expect(plannedMealTypeFromStored('FutureProviderType')).toBe('FutureProviderType');
    });
});
