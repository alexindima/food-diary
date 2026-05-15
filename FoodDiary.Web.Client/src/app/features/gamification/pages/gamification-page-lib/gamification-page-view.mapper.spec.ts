import { describe, expect, it } from 'vitest';

import type { Badge } from '../../models/gamification.data';
import {
    buildBadgeDisplays,
    buildGamificationStats,
    calculateHealthScoreRing,
    filterEarnedBadges,
    filterLockedBadges,
} from './gamification-page-view.mapper';

const HALF_SCORE = 50;
const FULL_SCORE = 100;
const OVER_MAX_SCORE = 150;
const NEGATIVE_SCORE = -10;
const CURRENT_STREAK = 3;
const LONGEST_STREAK = 12;
const TOTAL_MEALS_LOGGED = 48;
const WEEKLY_ADHERENCE = 86;
const BADGES: Badge[] = [
    { key: 'streak_3', category: 'streak', threshold: 3, isEarned: true },
    { key: 'meals_100', category: 'meals', threshold: 100, isEarned: false },
];

describe('gamification page view mapper', () => {
    it('calculates score ring values and clamps score to the valid range', () => {
        const halfRing = calculateHealthScoreRing(HALF_SCORE);
        const emptyRing = calculateHealthScoreRing(NEGATIVE_SCORE);
        const fullRing = calculateHealthScoreRing(OVER_MAX_SCORE);

        expect(halfRing.strokeDashoffset).toBeCloseTo(halfRing.strokeDasharray / 2);
        expect(emptyRing.strokeDashoffset).toBeCloseTo(emptyRing.strokeDasharray);
        expect(fullRing.strokeDashoffset).toBeCloseTo(calculateHealthScoreRing(FULL_SCORE).strokeDashoffset);
    });

    it('builds dashboard stat tiles', () => {
        const stats = buildGamificationStats(CURRENT_STREAK, LONGEST_STREAK, TOTAL_MEALS_LOGGED, WEEKLY_ADHERENCE);

        expect(stats).toEqual([
            expect.objectContaining({ key: 'currentStreak', value: '3', labelKey: 'GAMIFICATION.CURRENT_STREAK' }),
            expect.objectContaining({ key: 'longestStreak', value: '12', labelKey: 'GAMIFICATION.LONGEST_STREAK' }),
            expect.objectContaining({ key: 'totalMealsLogged', value: '48', labelKey: 'GAMIFICATION.TOTAL_MEALS' }),
            expect.objectContaining({ key: 'weeklyAdherence', value: '86%', labelKey: 'GAMIFICATION.WEEKLY_ADHERENCE' }),
        ]);
    });

    it('maps and splits badges by earned state', () => {
        const displays = buildBadgeDisplays(BADGES);

        expect(displays).toEqual([
            expect.objectContaining({
                key: 'streak_3',
                icon: 'local_fire_department',
                nameKey: 'GAMIFICATION.BADGE_STREAK_3',
            }),
            expect.objectContaining({
                key: 'meals_100',
                icon: 'restaurant',
                nameKey: 'GAMIFICATION.BADGE_MEALS_100',
            }),
        ]);
        expect(filterEarnedBadges(displays)).toEqual([displays[0]]);
        expect(filterLockedBadges(displays)).toEqual([displays[1]]);
    });
});
