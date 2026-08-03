import type { NutrientBar } from '../nutrition-summary/nutrition-summary.types';

const PERCENT = 100;
const SCALE_SMALL_OVERFLOW = 110;
const SCALE_MEDIUM_OVERFLOW = 125;
const SCALE_LARGE_OVERFLOW = 150;
const INITIAL_SCALE_STEPS = [SCALE_SMALL_OVERFLOW, SCALE_MEDIUM_OVERFLOW, SCALE_LARGE_OVERFLOW] as const;
const EXTENDED_SCALE_STEP = 25;
const WARNING_NUTRIENTS = new Set(['carbs', 'fats']);

export type DayNutrientBarViewModel = NutrientBar & {
    percent: number;
    fillWidth: number;
    isExcess: boolean;
};

export function calculateDaySummaryPercent(current: number, target: number): number {
    if (!Number.isFinite(current) || !Number.isFinite(target) || target <= 0) {
        return 0;
    }

    return Math.max(0, Math.round((current / target) * PERCENT));
}

export function resolveDaySummaryScaleMax(bars: NutrientBar[]): number {
    const highestPercent = Math.max(PERCENT, ...bars.map(bar => calculateDaySummaryPercent(bar.current, bar.target)));
    if (highestPercent <= PERCENT) {
        return PERCENT;
    }

    const initialStep = INITIAL_SCALE_STEPS.find(step => highestPercent <= step);

    return initialStep ?? Math.ceil(highestPercent / EXTENDED_SCALE_STEP) * EXTENDED_SCALE_STEP;
}

export function buildDayNutrientBarViewModels(bars: NutrientBar[], scaleMax: number): DayNutrientBarViewModel[] {
    return bars.map(bar => {
        const percent = calculateDaySummaryPercent(bar.current, bar.target);

        return {
            ...bar,
            percent,
            fillWidth: Math.min(PERCENT, (percent / scaleMax) * PERCENT),
            isExcess: WARNING_NUTRIENTS.has(bar.id) && percent > PERCENT,
        };
    });
}

export function calculateDaySummaryGoalPosition(scaleMax: number): number {
    return (PERCENT / Math.max(PERCENT, scaleMax)) * PERCENT;
}
