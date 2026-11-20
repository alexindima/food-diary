import { describe, expect, it } from 'vitest';

import { clampGoalValue, createDayCalories, createDayCaloriesFromGoals } from './goals-state.mapper';

const MAX_VALUE = 5000;
const CALORIE_TARGET = 2000;
const DECIMAL_VALUE = 10.6;
const ROUNDED_VALUE = 11;

describe('goals state mapper', () => {
    it('clamps and rounds values to explicit boundaries', () => {
        expect(clampGoalValue(-1, MAX_VALUE)).toBe(0);
        expect(clampGoalValue(MAX_VALUE + 1, MAX_VALUE)).toBe(MAX_VALUE);
        expect(clampGoalValue(DECIMAL_VALUE, MAX_VALUE)).toBe(ROUNDED_VALUE);
    });

    it('creates independent weekly calorie records and maps partial API values', () => {
        const first = createDayCalories(CALORIE_TARGET);
        const second = createDayCalories(CALORIE_TARGET);

        expect(first).toEqual(second);
        expect(first).not.toBe(second);
        expect(createDayCaloriesFromGoals({ mondayCalories: CALORIE_TARGET })).toMatchObject({
            mondayCalories: CALORIE_TARGET,
            tuesdayCalories: 0,
        });
        expect(createDayCaloriesFromGoals(null)).toEqual(createDayCalories(0));
    });
});
