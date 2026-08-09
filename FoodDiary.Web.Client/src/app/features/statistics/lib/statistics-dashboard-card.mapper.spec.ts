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
};

describe('statistics dashboard card mapper', () => {
    it('maps tracked days, real goals, missing days, nutrients, and body change', () => {
        const statistics: MappedStatistics = {
            date: [new Date('2026-08-02T00:00:00Z'), new Date('2026-08-03T00:00:00Z')],
            calories: [1900, 0],
            nutrientsStatistic: { proteins: [90, 0], fats: [60, 0], carbs: [220, 0], fiber: [20, 0] },
            aggregatedNutrients: { proteins: 90, fats: 60, carbs: 220, fiber: 20 },
        };

        const view = buildStatisticsDashboardCardsView({
            statistics,
            user: USER,
            bodyPoints: [
                { label: '2 Aug', value: 116 },
                { label: '3 Aug', value: 113 },
            ],
            periodDays: 7,
            formatDate: date => date.toISOString().slice(0, 10),
        });

        expect(view.overview.trackedDays).toBe(1);
        expect(view.overview.daysWithinGoal).toBe(1);
        expect(view.overview.averageCalories).toBe(1900);
        expect(view.overview.nutrients[0]).toEqual({ key: 'protein', current: 90, goal: 100 });
        expect(view.days[1]?.calories).toBeNull();
        expect(view.body).toMatchObject({ currentWeight: 113, change: -3, timeframeDays: 7 });
    });
});
