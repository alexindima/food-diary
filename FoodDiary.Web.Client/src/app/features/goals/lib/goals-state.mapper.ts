import type { DayCalorieKey, GoalsResponse } from '../../../shared/models/goals.data';

export type MacroKey = 'protein' | 'fats' | 'carbs' | 'fiber';
export type BodyTargetKey = 'weight' | 'waist';

export function clampGoalValue(value: number, max: number, min = 0): number {
    return Math.min(max, Math.max(min, Math.round(value)));
}

export function createDayCalories(value: number): Record<DayCalorieKey, number> {
    return {
        mondayCalories: value,
        tuesdayCalories: value,
        wednesdayCalories: value,
        thursdayCalories: value,
        fridayCalories: value,
        saturdayCalories: value,
        sundayCalories: value,
    };
}

export function createDayCaloriesFromGoals(goals: Partial<Pick<GoalsResponse, DayCalorieKey>> | null): Record<DayCalorieKey, number> {
    if (goals === null) {
        return createDayCalories(0);
    }

    return {
        mondayCalories: goals.mondayCalories ?? 0,
        tuesdayCalories: goals.tuesdayCalories ?? 0,
        wednesdayCalories: goals.wednesdayCalories ?? 0,
        thursdayCalories: goals.thursdayCalories ?? 0,
        fridayCalories: goals.fridayCalories ?? 0,
        saturdayCalories: goals.saturdayCalories ?? 0,
        sundayCalories: goals.sundayCalories ?? 0,
    };
}
