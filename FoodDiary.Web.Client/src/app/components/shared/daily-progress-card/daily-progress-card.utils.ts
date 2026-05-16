import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';

const MOTIVATION_THRESHOLDS: ReadonlyArray<{ maxPercent: number; key: string }> = [
    { maxPercent: 10, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P0_10' },
    { maxPercent: 20, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P10_20' },
    { maxPercent: 30, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P20_30' },
    { maxPercent: 40, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P30_40' },
    { maxPercent: 50, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P40_50' },
    { maxPercent: 60, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P50_60' },
    { maxPercent: 70, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P60_70' },
    { maxPercent: 80, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P70_80' },
    { maxPercent: 90, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P80_90' },
    { maxPercent: 110, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P90_110' },
    { maxPercent: 200, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P110_200' },
];

export function calculateDailyProgressPercent(consumed: number, goal: number): number {
    if (goal <= 0) {
        return 0;
    }

    return Math.round(Math.max(0, (consumed / goal) * PERCENT_MULTIPLIER));
}

export function calculateRemainingCalories(consumed: number, goal: number): number | null {
    if (goal <= 0) {
        return null;
    }

    const remaining = goal - consumed;
    return remaining > 0 ? remaining : 0;
}

export function resolveDailyProgressMotivationKey(consumed: number, goal: number): string | null {
    if (goal <= 0) {
        return null;
    }

    if (consumed <= 0) {
        return 'DAILY_PROGRESS_CARD.MOTIVATION.NONE';
    }

    const percent = (consumed / goal) * PERCENT_MULTIPLIER;
    return MOTIVATION_THRESHOLDS.find(item => percent <= item.maxPercent)?.key ?? 'DAILY_PROGRESS_CARD.MOTIVATION.ABOVE_200';
}
