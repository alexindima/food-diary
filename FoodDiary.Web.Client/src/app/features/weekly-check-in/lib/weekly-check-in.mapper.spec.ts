import { describe, expect, it } from 'vitest';

import type { WeekTrend } from '../models/weekly-check-in.data';
import {
    buildWeeklyCheckInSuggestionRows,
    buildWeeklyCheckInTrendCard,
    buildWeeklyCheckInTrendCards,
    getWeeklyCheckInTrendColor,
    getWeeklyCheckInTrendIcon,
} from './weekly-check-in.mapper';

const TREND: WeekTrend = {
    calorieChange: 120,
    proteinChange: -4.5,
    fatChange: 1,
    carbChange: 2,
    weightChange: -0.8,
    waistChange: null,
    hydrationChange: 250,
    mealsLoggedChange: 3,
};

describe('weekly check-in mapper', () => {
    it('builds suggestion rows from suggestion keys', () => {
        expect(buildWeeklyCheckInSuggestionRows(['ADD_PROTEIN'])).toEqual([
            {
                key: 'ADD_PROTEIN',
                labelKey: 'WEEKLY_CHECK_IN.ADD_PROTEIN',
            },
        ]);
    });

    it('builds trend cards with optional weight card', () => {
        const cards = buildWeeklyCheckInTrendCards(TREND);

        expect(cards.map(card => card.key)).toEqual(['calories', 'protein', 'weight', 'hydration']);
        expect(cards[0]).toMatchObject({
            key: 'calories',
            valuePrefix: '+',
            icon: 'trending_up',
            color: 'var(--fd-color-green-500)',
        });
        expect(cards[2]).toMatchObject({
            key: 'weight',
            icon: 'trending_down',
            color: 'var(--fd-color-green-500)',
        });
    });

    it('skips weight trend card when weight change is missing', () => {
        expect(buildWeeklyCheckInTrendCards({ ...TREND, weightChange: null }).map(card => card.key)).toEqual([
            'calories',
            'protein',
            'hydration',
        ]);
    });

    it('returns empty cards when trends are missing', () => {
        expect(buildWeeklyCheckInTrendCards(null)).toEqual([]);
        expect(buildWeeklyCheckInTrendCards(undefined)).toEqual([]);
    });

    it('builds single trend card formatting metadata', () => {
        const card = buildWeeklyCheckInTrendCard({
            key: 'protein',
            labelKey: 'WEEKLY_CHECK_IN.PROTEIN',
            value: -1,
            unitKey: 'GENERAL.UNITS.G',
            numberFormat: '1.1-1',
            unitSeparator: '',
        });

        expect(card).toEqual({
            key: 'protein',
            labelKey: 'WEEKLY_CHECK_IN.PROTEIN',
            value: -1,
            unitKey: 'GENERAL.UNITS.G',
            unitSeparator: '',
            numberFormat: '1.1-1',
            valuePrefix: '',
            color: 'var(--fd-color-danger)',
            icon: 'trending_down',
        });
    });

    it('maps trend icon and color by value direction', () => {
        expect(getWeeklyCheckInTrendIcon(1)).toBe('trending_up');
        expect(getWeeklyCheckInTrendIcon(-1)).toBe('trending_down');
        expect(getWeeklyCheckInTrendIcon(0)).toBe('trending_flat');
        expect(getWeeklyCheckInTrendColor(0)).toBe('var(--fd-color-slate-500)');
        expect(getWeeklyCheckInTrendColor(1)).toBe('var(--fd-color-green-500)');
        expect(getWeeklyCheckInTrendColor(-1)).toBe('var(--fd-color-danger)');
        expect(getWeeklyCheckInTrendColor(1, true)).toBe('var(--fd-color-danger)');
        expect(getWeeklyCheckInTrendColor(-1, true)).toBe('var(--fd-color-green-500)');
    });
});
