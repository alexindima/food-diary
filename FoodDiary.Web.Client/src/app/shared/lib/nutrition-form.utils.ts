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

export interface CalorieMismatchWarningInput {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    alcohol?: number;
    threshold?: number;
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
    return (
        normalizeMacroValue(proteins) * 4 +
        normalizeMacroValue(fats) * 9 +
        normalizeMacroValue(carbs) * 4 +
        normalizeMacroValue(alcohol) * 7
    );
}

/**
 * Calculate a calorie-mismatch warning when stated calories deviate from the
 * expected value (derived from macros) by more than `threshold` (default 20 %).
 *
 * Returns `null` when no warning should be shown (values are zero, missing, or
 * within the acceptable range).
 */
export function calculateCalorieMismatchWarning(input: CalorieMismatchWarningInput): CalorieMismatchWarning | null {
    const { calories, proteins, fats, carbs, alcohol = 0, threshold = 0.2 } = input;
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
export function calculateMacroBarState(proteins: number, fats: number, carbs: number): MacroBarState {
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

/**
 * Extract a non-negative numeric value from a form control.
 * Handles null, undefined, NaN, and negative values gracefully.
 */
export function getControlNumericValue(control: { value: number | string | null | undefined }): number {
    const raw = control.value;
    if (raw === null || raw === undefined || raw === '') {
        return 0;
    }
    const value = typeof raw === 'string' ? Number(raw.replace(',', '.').replace(/[^0-9.-]/g, '')) : Number(raw);
    return Number.isFinite(value) ? Math.max(0, value) : 0;
}

/**
 * Round a nutrient value to 2 decimal places.
 */
export function roundNutrient(value: number): number {
    return Math.round(value * 100) / 100;
}

/**
 * Check whether a manual calories error should be shown.
 * Returns a translation key or null.
 */
export function checkCaloriesError(control: { value: number | null; touched: boolean; dirty: boolean }): boolean {
    if (!control.touched && !control.dirty) {
        return false;
    }

    const value = Number(control.value);
    return !Number.isFinite(value) || value <= 0;
}

/**
 * Check whether a manual macros error should be shown (all macros are zero).
 */
export function checkMacrosError(controls: Array<{ value: number | null; touched: boolean; dirty: boolean }>): boolean {
    const shouldShow = controls.some(c => c.touched || c.dirty);
    if (!shouldShow) {
        return false;
    }

    return controls.every(c => {
        const value = Number(c.value);
        return !Number.isFinite(value) || value <= 0;
    });
}
