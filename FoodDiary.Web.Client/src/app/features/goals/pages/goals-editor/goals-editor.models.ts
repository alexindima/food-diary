import {
    CARB_CALORIES_PER_GRAM,
    FAT_CALORIES_PER_GRAM,
    PERCENT_MULTIPLIER,
    PROTEIN_CALORIES_PER_GRAM,
} from '../../../../shared/lib/nutrition.constants';
import type { BodyTargetKey, MacroKey, MacroPreset, MacroPresetKey } from '../../lib/goals.facade';
import type { DayCalorieKey, UpdateGoalsRequest } from '../../models/goals.data';

export type GoalsDraft = {
    calories: number;
    macros: Record<MacroKey, number>;
    preset: MacroPresetKey;
    water: number;
    bodyTargets: Record<BodyTargetKey, number>;
    cyclingEnabled: boolean;
    dayCalories: Record<DayCalorieKey, number>;
};

export type GoalsDraftChange = Partial<GoalsDraft>;

export type GoalsMacroDraft = {
    key: MacroKey;
    labelKey: string;
    unit: string;
    value: number;
    percent: number;
    accent: string;
    icon: string;
};

const FIBER_CALORIES_PER_GRAM = 2;

const MACRO_CALORIES_PER_GRAM: Record<MacroKey, number> = {
    protein: PROTEIN_CALORIES_PER_GRAM,
    fats: FAT_CALORIES_PER_GRAM,
    carbs: CARB_CALORIES_PER_GRAM,
    fiber: FIBER_CALORIES_PER_GRAM,
};

export function applyMacroPreset(draft: GoalsDraft, preset: MacroPreset): GoalsDraft {
    if (preset.percent === undefined) {
        return { ...draft, preset: preset.key };
    }

    return {
        ...draft,
        preset: preset.key,
        macros: {
            ...draft.macros,
            protein: Math.round((draft.calories * preset.percent.protein) / PROTEIN_CALORIES_PER_GRAM),
            fats: Math.round((draft.calories * preset.percent.fats) / FAT_CALORIES_PER_GRAM),
            carbs: Math.round((draft.calories * preset.percent.carbs) / CARB_CALORIES_PER_GRAM),
        },
    };
}

export function calculateMacroPercent(key: MacroKey, value: number, calories: number): number {
    if (calories <= 0) {
        return 0;
    }

    return Math.round((value * MACRO_CALORIES_PER_GRAM[key] * PERCENT_MULTIPLIER) / calories);
}

export function buildDraftRequest(draft: GoalsDraft): UpdateGoalsRequest {
    return {
        dailyCalorieTarget: draft.calories,
        proteinTarget: draft.macros.protein,
        fatTarget: draft.macros.fats,
        carbTarget: draft.macros.carbs,
        fiberTarget: draft.macros.fiber,
        waterGoal: draft.water,
        desiredWeight: normalizeDesiredBodyTarget(draft.bodyTargets.weight),
        desiredWaist: normalizeDesiredBodyTarget(draft.bodyTargets.waist),
        calorieCyclingEnabled: draft.cyclingEnabled,
        ...draft.dayCalories,
    };
}

function normalizeDesiredBodyTarget(value: number): number | null {
    return value > 0 ? value : null;
}
