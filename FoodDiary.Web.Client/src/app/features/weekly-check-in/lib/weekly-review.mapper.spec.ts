import { describe, expect, it } from 'vitest';

import type { WeekSummary, WeekTrend } from '../models/weekly-check-in.data';
import { buildWeeklyReview } from './weekly-review.mapper';

describe('weekly review mapper', () => {
    it('prioritizes data sufficiency and a logging focus for sparse weeks', () => {
        const review = buildWeeklyReview(createWeek({ daysLogged: 2 }), createTrends({ hydrationChange: 271 }), ['ADD_PROTEIN']);

        expect(review).toMatchObject({
            daysLogged: 2,
            hasEnoughData: false,
            summaryKey: 'WEEKLY_CHECK_IN.SUMMARY.LIMITED',
            focusTarget: 5,
            focusTitleKey: 'WEEKLY_CHECK_IN.FOCUS.LOGGING_TITLE',
        });
        expect(review?.insights.map(insight => insight.key)).toEqual(['hydration', 'protein', 'logging']);
        expect(review?.insights.slice(0, 2).map(insight => insight.key)).toEqual(['hydration', 'protein']);
        expect(Math.max((review?.insights.length ?? 0) - 2, 0)).toBe(1);
    });

    it('uses a consistency focus when the diary has enough entries', () => {
        const review = buildWeeklyReview(createWeek({ daysLogged: 6 }), createTrends(), []);

        expect(review).toMatchObject({
            hasEnoughData: true,
            summaryKey: 'WEEKLY_CHECK_IN.SUMMARY.RELIABLE',
            focusTarget: 7,
            focusTitleKey: 'WEEKLY_CHECK_IN.FOCUS.CONSISTENCY_TITLE',
        });
    });

    it('returns null until weekly data is available', () => {
        expect(buildWeeklyReview(void 0, void 0, [])).toBeNull();
    });
});

function createWeek(overrides: Partial<WeekSummary> = {}): WeekSummary {
    return {
        totalCalories: 1740,
        avgDailyCalories: 870,
        avgProteins: 34.6,
        avgFats: 33.6,
        avgCarbs: 111.4,
        mealsLogged: 3,
        daysLogged: 2,
        weightStart: 113,
        weightEnd: 113,
        waistStart: null,
        waistEnd: null,
        totalHydrationMl: 542,
        avgDailyHydrationMl: 271,
        ...overrides,
    };
}

function createTrends(overrides: Partial<WeekTrend> = {}): WeekTrend {
    return {
        calorieChange: -182,
        proteinChange: -1.2,
        fatChange: 0,
        carbChange: 0,
        weightChange: 0,
        waistChange: null,
        hydrationChange: 0,
        mealsLoggedChange: 0,
        ...overrides,
    };
}
