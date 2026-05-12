export const MEAL_TYPE_OPTIONS = ['BREAKFAST', 'LUNCH', 'DINNER', 'SNACK', 'OTHER'] as const;

export type MealTypeOption = (typeof MEAL_TYPE_OPTIONS)[number];

const MINUTES_PER_HOUR = 60;
const BREAKFAST_START_MINUTES = 300;
const LUNCH_START_MINUTES = 660;
const DINNER_START_MINUTES = 1020;
const SNACK_START_MINUTES = 1320;

export function isMealTypeOption(value: string): value is MealTypeOption {
    return MEAL_TYPE_OPTIONS.some(option => option === value);
}

export function normalizeMealType(value: string | null | undefined): MealTypeOption | null {
    const normalized = value?.trim().toUpperCase();
    if (normalized === undefined || normalized.length === 0) {
        return null;
    }

    return isMealTypeOption(normalized) ? normalized : null;
}

export function resolveMealTypeByTime(date: Date): MealTypeOption {
    const totalMinutes = date.getHours() * MINUTES_PER_HOUR + date.getMinutes();
    if (totalMinutes >= BREAKFAST_START_MINUTES && totalMinutes < LUNCH_START_MINUTES) {
        return 'BREAKFAST';
    }

    if (totalMinutes >= LUNCH_START_MINUTES && totalMinutes < DINNER_START_MINUTES) {
        return 'LUNCH';
    }

    if (totalMinutes >= DINNER_START_MINUTES && totalMinutes < SNACK_START_MINUTES) {
        return 'DINNER';
    }

    return 'SNACK';
}
