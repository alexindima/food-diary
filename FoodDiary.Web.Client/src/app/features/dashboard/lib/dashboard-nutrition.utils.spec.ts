import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';

import type { DashboardSnapshot, DashboardStatistics } from '../models/dashboard.data';
import {
    createConsumptionRingSignal,
    createMealPreviewSignal,
    createNutrientBarsSignal,
    placeholderIcon,
    placeholderLabel,
} from './dashboard-nutrition.utils';

const BAR_COUNT = 4;
const PREVIEW_WITHOUT_SLOTS_COUNT = 2;
const TODAY_SLOT_COUNT = 3;
const CALORIES = 2000;
const PROTEIN = 100;
const FATS = 70;
const CARBS = 250;
const FIBER = 25;
const PROTEIN_GOAL = 120;
const FAT_GOAL = 80;
const CARB_GOAL = 300;
const FIBER_GOAL = 30;
const WEEKLY_CALORIE_GOAL = 14000;
const DAILY_CONSUMED = 1500;
const WEEKLY_CONSUMED = 10000;

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
        weeklyCalorieGoal: 0,
        weeklyCalories: [],
        weight: { latest: null, previous: null, desired: null },
        waist: { latest: null, previous: null, desired: null },
        meals: { items: [], total: 0 },
        ...rest,
        statistics: stats,
    };
}

describe('dashboard-nutrition.utils', () => {
    registerPlaceholderTests();
    registerNutrientSignalTests();
    registerConsumptionRingTests();
    registerMealPreviewTests();
});

function registerPlaceholderTests(): void {
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
}

function registerNutrientSignalTests(): void {
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
                        totalCalories: CALORIES,
                        averageProteins: PROTEIN,
                        averageFats: FATS,
                        averageCarbs: CARBS,
                        averageFiber: FIBER,
                        proteinGoal: PROTEIN_GOAL,
                        fatGoal: FAT_GOAL,
                        carbGoal: CARB_GOAL,
                        fiberGoal: FIBER_GOAL,
                    },
                }),
            );

            const bars = createNutrientBarsSignal(snapshot);
            const result = bars();

            expect(result).toHaveLength(BAR_COUNT);
            expect(result[0].id).toBe('protein');
            expect(result[0].current).toBe(PROTEIN);
            expect(result[0].target).toBe(PROTEIN_GOAL);
            expect(result[1].id).toBe('carbs');
            expect(result[2].id).toBe('fats');
            expect(result[3].id).toBe('fiber');
        });
    });
}

function registerConsumptionRingTests(): void {
    describe('createConsumptionRingSignal', () => {
        it('should compute ring data from snapshot', () => {
            const snapshot = signal<DashboardSnapshot | null>(
                buildSnapshot({
                    dailyGoal: CALORIES,
                    weeklyCalorieGoal: WEEKLY_CALORIE_GOAL,
                    statistics: { totalCalories: DAILY_CONSUMED },
                }),
            );
            const weeklyConsumed = signal(WEEKLY_CONSUMED);
            const nutrientBars = signal([]);

            const ring = createConsumptionRingSignal(snapshot, weeklyConsumed, nutrientBars);
            const result = ring();

            expect(result.dailyGoal).toBe(CALORIES);
            expect(result.dailyConsumed).toBe(DAILY_CONSUMED);
            expect(result.weeklyConsumed).toBe(WEEKLY_CONSUMED);
            expect(result.weeklyGoal).toBe(WEEKLY_CALORIE_GOAL);
        });

        it('should handle null snapshot', () => {
            const ring = createConsumptionRingSignal(signal(null), signal(0), signal([]));
            expect(ring().dailyGoal).toBe(0);
            expect(ring().weeklyGoal).toBe(0);
        });
    });
}

function registerMealPreviewTests(): void {
    describe('createMealPreviewSignal', () => {
        it('should return meals without slots when not today', () => {
            const meals = signal([
                { id: '1', mealType: 'LUNCH' },
                { id: '2', mealType: 'DINNER' },
            ] as never[]);
            const isTodaySelected = signal(false);

            const preview = createMealPreviewSignal(meals, isTodaySelected);
            const result = preview();

            expect(result).toHaveLength(PREVIEW_WITHOUT_SLOTS_COUNT);
            expect(result[0].meal).toBeTruthy();
            expect(result[0].slot).toBe('LUNCH');
        });

        it('should fill empty slots when today is selected', () => {
            const meals = signal([{ id: '1', mealType: 'LUNCH' }] as never[]);
            const isTodaySelected = signal(true);

            const preview = createMealPreviewSignal(meals, isTodaySelected);
            const result = preview();

            expect(result.length).toBeGreaterThanOrEqual(TODAY_SLOT_COUNT);
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

            expect(result).toHaveLength(TODAY_SLOT_COUNT);
            expect(result.every(e => e.meal === null)).toBe(true);
        });
    });
}
