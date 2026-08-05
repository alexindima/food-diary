import { describe, expect, it } from 'vitest';

import { getWeightChangeTone, getWeightRemainingToGoal } from './weight-history-progress.utils';

const LOSS_CHANGE = -3;
const GAIN_CHANGE = 3;
const START_WEIGHT = 118;
const CURRENT_WEIGHT = 113;
const GAIN_GOAL = 172;
const LOSS_GOAL = 90;
const OVERSHOT_GAIN_WEIGHT = 175;
const GAIN_REMAINING = 59;
const LOSS_REMAINING = 23;

describe('weight history progress utils', () => {
    it('treats weight loss as negative when the goal requires weight gain', () => {
        expect(getWeightChangeTone(LOSS_CHANGE, CURRENT_WEIGHT, GAIN_GOAL)).toBe('negative');
        expect(getWeightChangeTone(GAIN_CHANGE, CURRENT_WEIGHT, GAIN_GOAL)).toBe('positive');
    });

    it('treats weight loss as positive when the goal requires weight loss', () => {
        expect(getWeightChangeTone(LOSS_CHANGE, CURRENT_WEIGHT, LOSS_GOAL)).toBe('positive');
        expect(getWeightChangeTone(GAIN_CHANGE, CURRENT_WEIGHT, LOSS_GOAL)).toBe('negative');
    });

    it('calculates remaining distance in either goal direction', () => {
        expect(getWeightRemainingToGoal(START_WEIGHT, CURRENT_WEIGHT, GAIN_GOAL)).toBe(GAIN_REMAINING);
        expect(getWeightRemainingToGoal(START_WEIGHT, CURRENT_WEIGHT, LOSS_GOAL)).toBe(LOSS_REMAINING);
        expect(getWeightRemainingToGoal(START_WEIGHT, OVERSHOT_GAIN_WEIGHT, GAIN_GOAL)).toBe(0);
    });
});
