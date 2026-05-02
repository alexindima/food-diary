export const MEAL_TYPE_OPTIONS = ['BREAKFAST', 'LUNCH', 'DINNER', 'SNACK', 'OTHER'] as const;

export type MealTypeOption = (typeof MEAL_TYPE_OPTIONS)[number];

export function normalizeMealType(value: string | null | undefined): MealTypeOption | null {
    const normalized = value?.trim().toUpperCase();
    if (!normalized) {
        return null;
    }

    return MEAL_TYPE_OPTIONS.includes(normalized as MealTypeOption) ? (normalized as MealTypeOption) : null;
}

export function resolveMealTypeByTime(date: Date): MealTypeOption {
    const totalMinutes = date.getHours() * 60 + date.getMinutes();
    if (totalMinutes >= 300 && totalMinutes < 660) {
        return 'BREAKFAST';
    }

    if (totalMinutes >= 660 && totalMinutes < 1020) {
        return 'LUNCH';
    }

    if (totalMinutes >= 1020 && totalMinutes < 1320) {
        return 'DINNER';
    }

    return 'SNACK';
}
