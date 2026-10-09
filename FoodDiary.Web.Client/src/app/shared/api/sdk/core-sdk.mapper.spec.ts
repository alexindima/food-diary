import { describe, expect, it } from 'vitest';

import { utcInstant } from '../../models/semantics/date-value';
import { entityId } from '../../models/semantics/entity-id';
import { dashboardSnapshotFromSdk } from './dashboard-sdk.mapper';
import type { DashboardSnapshotHttpResponse } from './generated/model/dashboard-snapshot-http-response';

describe('Dashboard SDK snapshots', () => {
    it('maps dashboard meals using saved snapshots while retaining their original instant and totals', () => {
        const instant = '2026-10-07T00:15:00+05:45';
        const mealCalories = 311.21;
        const itemAmount = 75.5;
        const response: DashboardSnapshotHttpResponse = {
            date: '2026-10-07',
            dateTo: '2026-10-07',
            dailyGoal: 2000,
            weeklyCalorieGoal: 14000,
            statistics: { totalCalories: 311.21, averageProteins: 0, averageFats: 1, averageCarbs: 2, averageFiber: 0 },
            weeklyCalories: [],
            weight: {},
            waist: {},
            meals: {
                total: 1,
                items: [
                    {
                        id: entityId<'meal'>('meal'),
                        date: utcInstant(instant),
                        isNutritionAutoCalculated: true,
                        totalCalories: 311.21,
                        totalProteins: 0,
                        totalFats: 1,
                        totalCarbs: 2,
                        totalFiber: 0,
                        totalAlcohol: 0,
                        items: [
                            {
                                id: 'item',
                                mealId: 'meal',
                                amount: 75.5,
                                productId: 'product',
                                productName: 'Saved product',
                                productBaseUnit: 'Ml',
                                productBaseAmount: 100,
                                productCaloriesPerBase: 2.73,
                            },
                        ],
                    },
                ],
            },
        };
        const meal = dashboardSnapshotFromSdk(response).meals.items[0];
        expect(meal.date).toBe(instant);
        expect(meal.totalCalories).toBe(mealCalories);
        expect(meal.items[0].amount).toBe(itemAmount);
        expect(meal.items[0].product).toMatchObject({
            id: 'product',
            name: 'Saved product',
            baseUnit: 'ML',
            baseAmount: 100,
            caloriesPerBase: 2.73,
        });
    });
});
