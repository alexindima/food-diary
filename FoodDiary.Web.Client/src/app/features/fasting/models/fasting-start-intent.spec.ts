import { describe, expect, it } from 'vitest';

import { MAX_CYCLIC_DAYS, MAX_FASTING_HOURS, MAX_INTERMITTENT_FAST_HOURS } from '../../../shared/lib/fasting.constants';
import { HOURS_PER_DAY } from '../../../shared/lib/time.constants';
import { extendedFastingHours, fastingCycleDays, fastingDailyWindow } from './fasting-start-intent';

const PRESET_FAST_HOURS = 16;
const FRACTIONAL_HOURS = 1.5;

describe('Fasting start quantities', () => {
    it.each([1, PRESET_FAST_HOURS, MAX_INTERMITTENT_FAST_HOURS])('derives a complementary daily window for %s hours', hours => {
        const window = fastingDailyWindow(hours);
        expect(window.fastHours).toBe(hours);
        expect(window.fastHours + window.eatingWindowHours).toBe(HOURS_PER_DAY);
        expect(Object.isFrozen(window)).toBe(true);
    });

    it.each([0, -1, FRACTIONAL_HOURS, Number.NaN, Number.POSITIVE_INFINITY])(
        'rejects an unnormalized value %s at the semantic boundary',
        value => {
            expect(() => fastingDailyWindow(value)).toThrow(RangeError);
            expect(() => fastingCycleDays(value)).toThrow(RangeError);
            expect(() => extendedFastingHours(value)).toThrow(RangeError);
        },
    );

    it('retains distinct existing bounds for daily hours, extended hours and cycle days', () => {
        expect(() => fastingDailyWindow(MAX_INTERMITTENT_FAST_HOURS + 1)).toThrow(RangeError);
        expect(extendedFastingHours(MAX_FASTING_HOURS)).toBe(MAX_FASTING_HOURS);
        expect(() => extendedFastingHours(MAX_FASTING_HOURS + 1)).toThrow(RangeError);
        expect(fastingCycleDays(MAX_CYCLIC_DAYS)).toBe(MAX_CYCLIC_DAYS);
        expect(() => fastingCycleDays(MAX_CYCLIC_DAYS + 1)).toThrow(RangeError);
    });
});
