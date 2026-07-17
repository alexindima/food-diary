import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import type { DayCalorieKey, GoalsResponse, UpdateGoalsRequest } from '../models/goals.data';

export type MacroKey = 'protein' | 'fats' | 'carbs' | 'fiber';
export type BodyTargetKey = 'weight' | 'waist';

type GoalsState = {
    calorieTarget: number;
    macros: Record<MacroKey, number>;
    waterValue: number;
    bodyTargets: Record<BodyTargetKey, number>;
    calorieCyclingEnabled: boolean;
    dayCalories: Record<DayCalorieKey, number>;
};

export function clampGoalValue(value: number, max: number, min = 0): number {
    return Math.min(max, Math.max(min, Math.round(value)));
}

export function calculateGoalProgressPercent(value: number, min: number, max: number): number {
    const span = max - min;
    if (span <= 0) {
        return 0;
    }

    const normalized = Math.min(Math.max(value - min, 0), span);
    return Math.round((normalized / span) * PERCENT_MULTIPLIER);
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

export function buildGoalsRequest(state: GoalsState): UpdateGoalsRequest {
    return {
        dailyCalorieTarget: state.calorieTarget,
        proteinTarget: state.macros.protein,
        fatTarget: state.macros.fats,
        carbTarget: state.macros.carbs,
        fiberTarget: state.macros.fiber,
        waterGoal: state.waterValue,
        desiredWeight: normalizeDesiredBodyTarget(state.bodyTargets.weight),
        desiredWaist: normalizeDesiredBodyTarget(state.bodyTargets.waist),
        calorieCyclingEnabled: state.calorieCyclingEnabled,
        ...(state.calorieCyclingEnabled && state.dayCalories),
    };
}

function normalizeDesiredBodyTarget(value: number): number | null {
    return value > 0 ? value : null;
}
