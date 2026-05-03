import { computed, type Signal } from '@angular/core';

import { type NutrientBar } from '../../../components/shared/dashboard-summary-card/dashboard-summary-card.component';
import { type MealPreviewEntry } from '../../../components/shared/meals-preview/meals-preview.component';
import { type Meal } from '../../meals/models/meal.data';
import { type DashboardSnapshot } from '../models/dashboard.data';

type MealSlot = 'BREAKFAST' | 'LUNCH' | 'DINNER';

const MEAL_SLOTS: MealSlot[] = ['BREAKFAST', 'LUNCH', 'DINNER'];

export function placeholderIcon(slot?: string | null): string {
    switch (slot) {
        case 'BREAKFAST':
            return 'wb_sunny';
        case 'LUNCH':
            return 'lunch_dining';
        case 'DINNER':
            return 'nights_stay';
        case 'SNACK':
            return 'cookie';
        case 'OTHER':
            return 'more_horiz';
        default:
            return 'restaurant_menu';
    }
}

export function placeholderLabel(slot?: string | null): string {
    if (!slot) {
        return 'MEAL_CARD.MEAL_TYPES.OTHER';
    }
    return `MEAL_CARD.MEAL_TYPES.${slot}`;
}

export function createNutrientBarsSignal(snapshot: Signal<DashboardSnapshot | null>): Signal<NutrientBar[]> {
    return computed<NutrientBar[]>(() => {
        const s = snapshot();
        if (!s) {
            return [];
        }

        return [
            {
                id: 'protein',
                label: 'Protein',
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                current: s.statistics.averageProteins,
                target: s.statistics.proteinGoal ?? 0,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-gradient-brand-start)',
                colorEnd: 'var(--fd-color-primary-600)',
            },
            {
                id: 'carbs',
                label: 'Carbs',
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                current: s.statistics.averageCarbs,
                target: s.statistics.carbGoal ?? 0,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-teal-500)',
                colorEnd: 'var(--fd-color-sky-500)',
            },
            {
                id: 'fats',
                label: 'Fats',
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                current: s.statistics.averageFats,
                target: s.statistics.fatGoal ?? 0,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-amber-500)',
                colorEnd: 'var(--fd-color-orange-500)',
            },
            {
                id: 'fiber',
                label: 'Fiber',
                labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
                current: s.statistics.averageFiber,
                target: s.statistics.fiberGoal ?? 0,
                unit: 'g',
                unitKey: 'GENERAL.UNITS.G',
                colorStart: 'var(--fd-color-rose-500)',
                colorEnd: 'var(--fd-color-rose-500)',
            },
        ];
    });
}

export function createConsumptionRingSignal(
    snapshot: Signal<DashboardSnapshot | null>,
    weeklyConsumed: Signal<number>,
    nutrientBars: Signal<NutrientBar[]>,
): Signal<{
    dailyGoal: number;
    dailyConsumed: number;
    weeklyConsumed: number;
    weeklyGoal: number;
    nutrientBars: NutrientBar[];
}> {
    return computed(() => {
        const s = snapshot();
        const dailyGoal = s?.dailyGoal ?? 0;
        const consumedToday = s?.statistics.totalCalories ?? 0;

        return {
            dailyGoal,
            dailyConsumed: consumedToday,
            weeklyConsumed: weeklyConsumed(),
            weeklyGoal: s ? s.weeklyCalorieGoal : 0,
            nutrientBars: nutrientBars(),
        };
    });
}

export function createMealPreviewSignal(meals: Signal<Meal[]>, isTodaySelected: Signal<boolean>): Signal<MealPreviewEntry[]> {
    return computed<MealPreviewEntry[]>(() => {
        const mealList = [...meals()];

        if (!isTodaySelected()) {
            return mealList.map(meal => ({
                meal,
                slot: meal.mealType ?? undefined,
            }));
        }

        const result: { meal: Meal | null; slot?: MealSlot }[] = [];

        for (const slot of MEAL_SLOTS) {
            const index = mealList.findIndex(m => (m.mealType ?? '').toUpperCase() === slot);
            if (index >= 0) {
                result.push({ meal: mealList[index], slot });
                mealList.splice(index, 1);
            } else {
                result.push({ meal: null, slot });
            }
        }

        for (const meal of mealList) {
            result.push({ meal, slot: (meal.mealType ?? '').toUpperCase() as MealSlot | undefined });
        }

        return result.map(entry => ({
            meal: entry.meal ?? null,
            slot: entry.slot,
            icon: placeholderIcon(entry.slot),
            labelKey: placeholderLabel(entry.slot),
        }));
    });
}
