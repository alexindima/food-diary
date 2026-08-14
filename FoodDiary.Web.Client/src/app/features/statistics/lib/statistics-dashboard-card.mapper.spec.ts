import { describe, expect, it } from 'vitest';

/* eslint-disable @typescript-eslint/no-magic-numbers -- Compact fixture values make the mapper expectations readable. */
import type { User } from '../../../shared/models/user.data';
import type { MappedStatistics } from '../models/statistics.data';
import { buildStatisticsDashboardCardsView } from './statistics-dashboard-card.mapper';

const USER: User = {
    id: 'user-id',
    email: 'user@example.com',
    hasPassword: true,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: 0,
    fastingCheckInFollowUpReminderHours: 0,
    isActive: true,
    isEmailConfirmed: true,
    dailyCalorieTarget: 2000,
    proteinTarget: 100,
    fatTarget: 70,
    carbTarget: 250,
    fiberTarget: 25,
    desiredWeightKg: 75,
    desiredWaistCm: 80,
};

describe('statistics dashboard card mapper', () => {
    it('maps tracked days, real goals, missing days, nutrients, and body change', () => {
        const statistics: MappedStatistics = {
            date: [new Date('2026-08-02T00:00:00Z'), new Date('2026-08-03T00:00:00Z')],
            calories: [1900, 0],
            nutrientsStatistic: { proteins: [90, 0], fats: [60, 0], carbs: [220, 0], fiber: [20, 0] },
            aggregatedNutrients: { proteins: 90, fats: 60, carbs: 220, fiber: 20 },
            mealStructure: {
                breakfastCalories: 400,
                lunchCalories: 800,
                dinnerCalories: 600,
                snackCalories: 200,
                mealCount: 7,
                trackedDayCount: 2,
            },
        };

        const view = buildStatisticsDashboardCardsView({
            statistics,
            user: USER,
            weightPoints: [
                { label: '2 Aug', value: 116 },
                { label: '3 Aug', value: 113 },
            ],
            waistPoints: [
                { label: '2 Aug', value: 101 },
                { label: '3 Aug', value: 99 },
            ],
            quantizationDays: 1,
            periodDays: 7,
            formatDate: date => date.toISOString().slice(0, 10),
        });

        expect(view.overview.trackedDays).toBe(1);
        expect(view.overview.daysWithinGoal).toBe(1);
        expect(view.overview.averageCalories).toBe(1900);
        expect(view.overview.nutrients[0]).toEqual({ key: 'protein', current: 90, goal: 100 });
        expect(view.days[1]?.calories).toBeNull();
        expect(view.body.weight).toMatchObject({ key: 'weight', current: 113, change: -3, goal: 75, timeframeDays: 7 });
        expect(view.body.waist).toMatchObject({ key: 'waist', current: 99, change: -2, goal: 80, timeframeDays: 7 });
        expect(view.stability).toMatchObject({
            stableCount: 1,
            totalCount: 2,
            averageDeviationPercent: 5,
            longestLoggingStreak: 1,
            usesDailyIntervals: true,
        });
        expect(view.mealStructure).toMatchObject({
            totalCalories: 1000,
            averageMealsPerDay: 3.5,
            dominantMeal: 'lunch',
        });
        expect(view.mealStructure.items).toEqual([
            { key: 'breakfast', calories: 200, percentage: 20 },
            { key: 'lunch', calories: 400, percentage: 40 },
            { key: 'dinner', calories: 300, percentage: 30 },
            { key: 'snack', calories: 100, percentage: 10 },
        ]);
    });
});

describe('statistics meal structure mapper', () => {
    it('allocates rounded meal shares to exactly one hundred percent', () => {
        const statistics: MappedStatistics = {
            date: [new Date('2026-08-09T00:00:00Z')],
            calories: [792],
            nutrientsStatistic: { proteins: [0], fats: [0], carbs: [0], fiber: [0] },
            aggregatedNutrients: { proteins: 0, fats: 0, carbs: 0, fiber: 0 },
            mealStructure: {
                breakfastCalories: 58,
                lunchCalories: 149,
                dinnerCalories: 312,
                snackCalories: 273,
                mealCount: 4,
                trackedDayCount: 1,
            },
        };

        const view = buildStatisticsDashboardCardsView({
            statistics,
            user: USER,
            weightPoints: [],
            waistPoints: [],
            quantizationDays: 1,
            periodDays: 1,
            formatDate: date => date.toISOString().slice(0, 10),
        });

        const percentages = view.mealStructure.items.map(item => item.percentage);
        expect(percentages).toEqual([7, 19, 39, 35]);
        expect(percentages.reduce((sum, value) => sum + value, 0)).toBe(100);
    });
});
