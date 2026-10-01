import { describe, expect, it } from 'vitest';

import type { MacroPreset } from '../../lib/goals.facade';
import { applyMacroPreset, areBodyTargetsValid, buildDraftRequest, calculateMacroPercent, type GoalsDraft } from './goals-editor.models';

const WEIGHT_LIMIT = 500;
const WAIST_LIMIT = 300;
const LIMIT_STEP = 0.01;
const CALORIES = 2258;
const WEIGHT = 75;
const CLASSIC_PROTEIN_PERCENT = 30;
const CLASSIC_FATS_PERCENT = 30;
const CLASSIC_CARBS_PERCENT = 40;
const CLASSIC_PRESET: MacroPreset = {
    key: 'classic',
    labelKey: 'GOALS_PAGE.MACRO_PRESET_CLASSIC',
    percent: { protein: 0.3, fats: 0.3, carbs: 0.4 },
};

describe('goals page v2 draft calculations', () => {
    it.each([
        [0, 0, true],
        [WEIGHT_LIMIT, WAIST_LIMIT, true],
        [WEIGHT_LIMIT + LIMIT_STEP, WAIST_LIMIT, false],
        [WEIGHT_LIMIT, WAIST_LIMIT + LIMIT_STEP, false],
        [-1, 0, false],
        [0, -1, false],
        [Number.NaN, 0, false],
        [0, Number.POSITIVE_INFINITY, false],
    ])('validates canonical body limits for weight %s and waist %s', (weight, waist, expected) => {
        expect(areBodyTargetsValid({ weight, waist })).toBe(expected);
    });

    it('recalculates macros from the selected preset and preserves fiber', () => {
        const result = applyMacroPreset(createDraft(), CLASSIC_PRESET);

        expect(result.preset).toBe('classic');
        expect(result.macros).toEqual({ protein: 169, fats: 75, carbs: 226, fiber: 11 });
    });

    it('calculates the displayed calorie ratio from current draft values', () => {
        const result = applyMacroPreset(createDraft(), CLASSIC_PRESET);

        expect(calculateMacroPercent('protein', result.macros.protein, result.calories)).toBe(CLASSIC_PROTEIN_PERCENT);
        expect(calculateMacroPercent('fats', result.macros.fats, result.calories)).toBe(CLASSIC_FATS_PERCENT);
        expect(calculateMacroPercent('carbs', result.macros.carbs, result.calories)).toBe(CLASSIC_CARBS_PERCENT);
    });

    it('sends unset body targets as null', () => {
        const request = buildDraftRequest(createDraft());

        expect(request.desiredWeightKg).toBe(WEIGHT);
        expect(request.desiredWaistCm).toBeNull();
    });
});

function createDraft(): GoalsDraft {
    return {
        calories: CALORIES,
        macros: { protein: 220, fats: 75, carbs: 113, fiber: 11 },
        preset: 'custom',
        water: 2000,
        bodyTargets: { weight: WEIGHT, waist: 0 },
        cyclingEnabled: false,
        dayCalories: {
            mondayCalories: CALORIES,
            tuesdayCalories: CALORIES,
            wednesdayCalories: CALORIES,
            thursdayCalories: CALORIES,
            fridayCalories: CALORIES,
            saturdayCalories: CALORIES,
            sundayCalories: CALORIES,
        },
    };
}
