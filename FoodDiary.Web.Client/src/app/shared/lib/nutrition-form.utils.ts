export type MacroKey = 'proteins' | 'fats' | 'carbs';

export interface MacroBarSegment {
    key: MacroKey;
    percent: number;
}

export interface MacroBarState {
    isEmpty: boolean;
    segments: MacroBarSegment[];
}

export interface CalorieMismatchWarning {
    expectedCalories: number;
    actualCalories: number;
}

export type NutritionMode = 'auto' | 'manual';

/**
 * Calculate expected calories from macronutrient grams.
 * Proteins: 4 kcal/g, Fats: 9 kcal/g, Carbs: 4 kcal/g, Alcohol: 7 kcal/g.
 *
 * Delegates to the same normalization logic used by {@link NutritionCalculationService}:
 * null / undefined / NaN / Infinity / negative values are treated as 0.
 */
export function calculateCaloriesFromMacros(
    proteins: number | null | undefined,
    fats: number | null | undefined,
    carbs: number | null | undefined,
    alcohol: number | null | undefined = 0,
): number {
    return normalizeMacroValue(proteins) * 4
        + normalizeMacroValue(fats) * 9
        + normalizeMacroValue(carbs) * 4
        + normalizeMacroValue(alcohol) * 7;
}

/**
 * Calculate a calorie-mismatch warning when stated calories deviate from the
 * expected value (derived from macros) by more than `threshold` (default 20 %).
 *
 * Returns `null` when no warning should be shown (values are zero, missing, or
 * within the acceptable range).
 */
export function calculateCalorieMismatchWarning(
    calories: number,
    proteins: number,
    fats: number,
    carbs: number,
    alcohol: number = 0,
    threshold: number = 0.2,
): CalorieMismatchWarning | null {
    const expectedCalories = calculateCaloriesFromMacros(proteins, fats, carbs, alcohol);

    if (expectedCalories <= 0 || calories <= 0) {
        return null;
    }

    const deviation = Math.abs(calories - expectedCalories) / expectedCalories;
    if (deviation <= threshold) {
        return null;
    }

    return {
        expectedCalories: Math.round(expectedCalories),
        actualCalories: Math.round(calories),
    };
}

/**
 * Build the macro-distribution bar state from protein / fat / carb gram values.
 *
 * Only macros with a positive value produce a segment. When every value is zero
 * (or negative) the returned state is `{ isEmpty: true, segments: [] }`.
 */
export function calculateMacroBarState(
    proteins: number,
    fats: number,
    carbs: number,
): MacroBarState {
    const entries: Array<{ key: MacroKey; value: number }> = [
        { key: 'proteins', value: proteins },
        { key: 'fats', value: fats },
        { key: 'carbs', value: carbs },
    ];

    const positive = entries.filter(entry => entry.value > 0);
    if (positive.length === 0) {
        return { isEmpty: true, segments: [] };
    }

    const total = positive.reduce((sum, entry) => sum + entry.value, 0);
    return {
        isEmpty: false,
        segments: positive.map(entry => ({
            key: entry.key,
            percent: (entry.value / total) * 100,
        })),
    };
}

function normalizeMacroValue(value: number | null | undefined): number {
    if (typeof value !== 'number' || !Number.isFinite(value)) {
        return 0;
    }
    return Math.max(0, value);
}
