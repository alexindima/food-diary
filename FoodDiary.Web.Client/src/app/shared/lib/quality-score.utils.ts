export const QUALITY_SCORE_MIN = 0;
export const QUALITY_SCORE_MAX = 100;
export const DEFAULT_QUALITY_SCORE = 50;

export function normalizeQualityScore(score: number | null | undefined, fallback = DEFAULT_QUALITY_SCORE): number {
    return Math.round(Math.min(QUALITY_SCORE_MAX, Math.max(QUALITY_SCORE_MIN, score ?? fallback)));
}
