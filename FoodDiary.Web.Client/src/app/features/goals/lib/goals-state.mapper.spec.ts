import { describe, expect, it } from 'vitest';

import {
    buildGoalsRequest,
    calculateGoalProgressPercent,
    clampGoalValue,
    createDayCalories,
    createDayCaloriesFromGoals,
} from './goals-state.mapper';

const MAX_VALUE = 5000;
const CALORIE_TARGET = 2000;
const MIDPOINT_PROGRESS = 50;
const FULL_PROGRESS = 100;
const DECIMAL_VALUE = 10.6;
const ROUNDED_VALUE = 11;
const WAIST_TARGET = 80;

describe('goals state mapper', () => {
    it('clamps and rounds values to explicit boundaries', () => {
        expect(clampGoalValue(-1, MAX_VALUE)).toBe(0);
        expect(clampGoalValue(MAX_VALUE + 1, MAX_VALUE)).toBe(MAX_VALUE);
        expect(clampGoalValue(DECIMAL_VALUE, MAX_VALUE)).toBe(ROUNDED_VALUE);
    });

    it('calculates bounded progress and handles invalid ranges', () => {
        expect(calculateGoalProgressPercent(CALORIE_TARGET, 0, CALORIE_TARGET * 2)).toBe(MIDPOINT_PROGRESS);
        expect(calculateGoalProgressPercent(-1, 0, CALORIE_TARGET)).toBe(0);
        expect(calculateGoalProgressPercent(MAX_VALUE, 0, CALORIE_TARGET)).toBe(FULL_PROGRESS);
        expect(calculateGoalProgressPercent(1, 1, 1)).toBe(0);
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

    it('includes weekday targets only when calorie cycling is enabled', () => {
        const baseState = {
            calorieTarget: CALORIE_TARGET,
            macros: { protein: 100, fats: 70, carbs: 250, fiber: 30 },
            waterValue: 2200,
            bodyTargets: { weight: 0, waist: WAIST_TARGET },
            dayCalories: createDayCalories(CALORIE_TARGET),
        };

        const regular = buildGoalsRequest({ ...baseState, calorieCyclingEnabled: false });
        const cycling = buildGoalsRequest({ ...baseState, calorieCyclingEnabled: true });

        expect(regular.desiredWeight).toBeNull();
        expect(regular.desiredWaist).toBe(WAIST_TARGET);
        expect(regular.mondayCalories).toBeUndefined();
        expect(cycling.mondayCalories).toBe(CALORIE_TARGET);
        expect(cycling.sundayCalories).toBe(CALORIE_TARGET);
    });
});
