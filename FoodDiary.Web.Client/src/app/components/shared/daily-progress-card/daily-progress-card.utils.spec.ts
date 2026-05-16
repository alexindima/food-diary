import { describe, expect, it } from 'vitest';

import { calculateDailyProgressPercent, calculateRemainingCalories, resolveDailyProgressMotivationKey } from './daily-progress-card.utils';

const DAILY_GOAL = 2000;
const CONSUMED_QUARTER = 500;
const CONSUMED_THIRD = 333;
const ROUNDING_GOAL = 1000;
const CONSUMED_OVER_GOAL = 3000;
const CONSUMED_ABOVE_TWO_HUNDRED = 5000;
const NEGATIVE_CONSUMED = -100;
const QUARTER_PROGRESS = 25;
const THIRD_PROGRESS = 33;
const OVER_GOAL_PROGRESS = 150;
const REMAINING_CALORIES = 1500;

describe('daily progress card utils', () => {
    it('calculates rounded progress and clamps negative consumption', () => {
        expect(calculateDailyProgressPercent(CONSUMED_QUARTER, DAILY_GOAL)).toBe(QUARTER_PROGRESS);
        expect(calculateDailyProgressPercent(CONSUMED_THIRD, ROUNDING_GOAL)).toBe(THIRD_PROGRESS);
        expect(calculateDailyProgressPercent(NEGATIVE_CONSUMED, DAILY_GOAL)).toBe(0);
        expect(calculateDailyProgressPercent(CONSUMED_OVER_GOAL, DAILY_GOAL)).toBe(OVER_GOAL_PROGRESS);
    });

    it('calculates remaining calories only when a goal exists', () => {
        expect(calculateRemainingCalories(CONSUMED_QUARTER, DAILY_GOAL)).toBe(REMAINING_CALORIES);
        expect(calculateRemainingCalories(CONSUMED_OVER_GOAL, DAILY_GOAL)).toBe(0);
        expect(calculateRemainingCalories(CONSUMED_QUARTER, 0)).toBeNull();
    });

    it('resolves motivation keys for empty, in-range, and above-range progress', () => {
        expect(resolveDailyProgressMotivationKey(CONSUMED_QUARTER, 0)).toBeNull();
        expect(resolveDailyProgressMotivationKey(0, DAILY_GOAL)).toBe('DAILY_PROGRESS_CARD.MOTIVATION.NONE');
        expect(resolveDailyProgressMotivationKey(CONSUMED_QUARTER, DAILY_GOAL)).toBe('DAILY_PROGRESS_CARD.MOTIVATION.P20_30');
        expect(resolveDailyProgressMotivationKey(CONSUMED_ABOVE_TWO_HUNDRED, DAILY_GOAL)).toBe('DAILY_PROGRESS_CARD.MOTIVATION.ABOVE_200');
    });
});
