import type { TdeeInsight } from '../models/tdee-insight.data';

export const TDEE_SUGGESTION_DIFF_THRESHOLD = 50;
export const TDEE_WEIGHT_TREND_FRACTION_DIGITS = 2;

export function getEffectiveTdee(insight: TdeeInsight | null | undefined): number | null {
    return insight?.adaptiveTdee ?? insight?.estimatedTdee ?? null;
}

export function formatTdeeWeightTrend(value: number | null | undefined): string | null {
    if (value === null || value === undefined) {
        return null;
    }

    const sign = value > 0 ? '+' : '';
    return `${sign}${value.toFixed(TDEE_WEIGHT_TREND_FRACTION_DIGITS)}`;
}

export function buildTdeeHintKey(hint: string | null | undefined): string | null {
    const value = hint ?? '';
    return value.length > 0 ? `TDEE_CARD.HINTS.${value.replace('hint.', '').toUpperCase()}` : null;
}

export function buildTdeeConfidenceKey(insight: TdeeInsight | null, emptyConfidenceKey: string): string {
    return insight !== null && insight.confidence !== 'none'
        ? `TDEE_CARD.CONFIDENCE.${insight.confidence.toUpperCase()}`
        : emptyConfidenceKey;
}

export function hasMeaningfulTdeeSuggestion(insight: TdeeInsight | null): boolean {
    const suggested = insight?.suggestedCalorieTarget ?? 0;
    const current = insight?.currentCalorieTarget ?? 0;
    return suggested > 0 && current > 0 && Math.abs(suggested - current) > TDEE_SUGGESTION_DIFF_THRESHOLD;
}
