export const DEFAULT_SATIETY_LEVEL = 3;
export const MIN_SATIETY_LEVEL = 1;
export const MAX_SATIETY_LEVEL = 5;
export const LEGACY_SATIETY_SCALE_FACTOR = 2;

export function normalizeSatietyLevel(value: number | null | undefined): number {
    if (value === null || value === undefined || !Number.isFinite(value) || value <= 0) {
        return DEFAULT_SATIETY_LEVEL;
    }

    if (value > MAX_SATIETY_LEVEL) {
        return Math.min(MAX_SATIETY_LEVEL, Math.max(MIN_SATIETY_LEVEL, Math.round(value / LEGACY_SATIETY_SCALE_FACTOR)));
    }

    return Math.max(MIN_SATIETY_LEVEL, value);
}
