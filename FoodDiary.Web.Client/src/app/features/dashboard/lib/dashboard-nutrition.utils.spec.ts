import { describe, it, expect } from 'vitest';
import { signal } from '@angular/core';
import {
    placeholderIcon,
    placeholderLabel,
    createNutrientBarsSignal,
    createConsumptionRingSignal,
    createMealPreviewSignal,
} from './dashboard-nutrition.utils';
import { DashboardSnapshot, DashboardStatistics } from '../models/dashboard.data';

function buildSnapshot(
    overrides: { statistics: Partial<DashboardStatistics> } & Partial<Omit<DashboardSnapshot, 'statistics'>>,
): DashboardSnapshot {
    const { statistics: statsOverrides, ...rest } = overrides;
    const stats: DashboardStatistics = {
        totalCalories: 0,
        averageProteins: 0,
        averageFats: 0,
        averageCarbs: 0,
        averageFiber: 0,
        ...statsOverrides,
    };
    return {
        date: '2026-03-15',
        dailyGoal: 0,
        weeklyCalories: [],
        weight: { latest: null, previous: null, desired: null },
        waist: { latest: null, previous: null, desired: null },
        meals: { items: [], total: 0 },
        ...rest,
        statistics: stats,
    };
}

describe('dashboard-nutrition.utils', () => {
    describe('placeholderIcon', () => {
        it('should return correct icon for BREAKFAST', () => {
            expect(placeholderIcon('BREAKFAST')).toBe('wb_sunny');
        });

        it('should return correct icon for LUNCH', () => {
            expect(placeholderIcon('LUNCH')).toBe('lunch_dining');
        });

        it('should return correct icon for DINNER', () => {
            expect(placeholderIcon('DINNER')).toBe('nights_stay');
        });

        it('should return correct icon for SNACK', () => {
            expect(placeholderIcon('SNACK')).toBe('cookie');
        });

        it('should return correct icon for OTHER', () => {
            expect(placeholderIcon('OTHER')).toBe('more_horiz');
        });

        it('should return default icon for unknown slot', () => {
            expect(placeholderIcon('UNKNOWN')).toBe('restaurant_menu');
        });

        it('should return default icon for null', () => {
            expect(placeholderIcon(null)).toBe('restaurant_menu');
        });

        it('should return default icon for undefined', () => {
            expect(placeholderIcon(undefined)).toBe('restaurant_menu');
        });
    });

    describe('placeholderLabel', () => {
        it('should return meal type key for known slot', () => {
            expect(placeholderLabel('BREAKFAST')).toBe('MEAL_CARD.MEAL_TYPES.BREAKFAST');
        });

        it('should return OTHER key for null/undefined', () => {
            expect(placeholderLabel(null)).toBe('MEAL_CARD.MEAL_TYPES.OTHER');
            expect(placeholderLabel(undefined)).toBe('MEAL_CARD.MEAL_TYPES.OTHER');
        });
    });

    describe('createNutrientBarsSignal', () => {
        it('should return empty array when snapshot is null', () => {
            const snapshot = signal<DashboardSnapshot | null>(null);
            const bars = createNutrientBarsSignal(snapshot);
            expect(bars()).toEqual([]);
        });

        it('should return 4 nutrient bars from snapshot', () => {
            const snapshot = signal<DashboardSnapshot | null>(
                buildSnapshot({
                    statistics: {
                        totalCalories: 2000,
                        averageProteins: 100,
                        averageFats: 70,
                        averageCarbs: 250,
                        averageFiber: 25,
                        proteinGoal: 120,
                        fatGoal: 80,
                        carbGoal: 300,
                        fiberGoal: 30,
                    },
                }),
            );

            const bars = createNutrientBarsSignal(snapshot);
            const result = bars();

            expect(result).toHaveLength(4);
            expect(result[0].id).toBe('protein');
            expect(result[0].current).toBe(100);
            expect(result[0].target).toBe(120);
            expect(result[1].id).toBe('carbs');
            expect(result[2].id).toBe('fats');
            expect(result[3].id).toBe('fiber');
        });
    });

    describe('createConsumptionRingSignal', () => {
        it('should compute ring data from snapshot', () => {
            const snapshot = signal<DashboardSnapshot | null>(
                buildSnapshot({
                    dailyGoal: 2000,
                    statistics: { totalCalories: 1500 },
                }),
            );
            const weeklyConsumed = signal(10000);
            const nutrientBars = signal([]);

            const ring = createConsumptionRingSignal(snapshot, weeklyConsumed, nutrientBars);
            const result = ring();

            expect(result.dailyGoal).toBe(2000);
            expect(result.dailyConsumed).toBe(1500);
            expect(result.weeklyConsumed).toBe(10000);
            expect(result.weeklyGoal).toBe(14000);
        });

        it('should handle null snapshot', () => {
            const ring = createConsumptionRingSignal(signal(null), signal(0), signal([]));
            expect(ring().dailyGoal).toBe(0);
            expect(ring().weeklyGoal).toBe(0);
        });
    });

    describe('createMealPreviewSignal', () => {
        it('should return meals without slots when not today', () => {
            const meals = signal([
                { id: '1', mealType: 'LUNCH' },
                { id: '2', mealType: 'DINNER' },
            ] as never[]);
            const isTodaySelected = signal(false);

            const preview = createMealPreviewSignal(meals, isTodaySelected);
            const result = preview();

            expect(result).toHaveLength(2);
            expect(result[0].meal).toBeTruthy();
            expect(result[0].slot).toBe('LUNCH');
        });

        it('should fill empty slots when today is selected', () => {
            const meals = signal([{ id: '1', mealType: 'LUNCH' }] as never[]);
            const isTodaySelected = signal(true);

            const preview = createMealPreviewSignal(meals, isTodaySelected);
            const result = preview();

            expect(result.length).toBeGreaterThanOrEqual(3);
            const breakfastSlot = result.find(e => e.slot === 'BREAKFAST');
            expect(breakfastSlot?.meal).toBeNull();
            const lunchSlot = result.find(e => e.slot === 'LUNCH');
            expect(lunchSlot?.meal).toBeTruthy();
        });

        it('should return empty placeholder slots when no meals today', () => {
            const meals = signal([] as never[]);
            const isTodaySelected = signal(true);

            const preview = createMealPreviewSignal(meals, isTodaySelected);
            const result = preview();

            expect(result).toHaveLength(3);
            expect(result.every(e => e.meal === null)).toBe(true);
        });
    });
});
