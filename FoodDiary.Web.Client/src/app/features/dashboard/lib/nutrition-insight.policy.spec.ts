import { describe, expect, it } from 'vitest';

import type { DashboardSnapshot } from '../../../shared/models/dashboard.data';
import { type NutritionInsightInput, resolveDashboardNutritionInsight, resolveNutritionInsight } from './nutrition-insight.policy';

const BASE_INPUT: NutritionInsightInput = {
    mealCount: 3,
    totals: { calories: 1800, proteins: 100, fats: 70, carbs: 220, fiber: 25 },
    goals: { calories: 2200, proteins: 120, fats: 80, carbs: 260, fiber: 30 },
};

describe('resolveNutritionInsight', () => {
    it('returns an empty state when no meals were logged', () => {
        expect(resolveNutritionInsight({ ...BASE_INPUT, mealCount: 0 }).kind).toBe('empty');
    });

    it('prioritizes a material calorie excess', () => {
        const result = resolveNutritionInsight({ ...BASE_INPUT, totals: { ...BASE_INPUT.totals, calories: 2500 } });

        expect(result).toMatchObject({ kind: 'calorie-excess', tone: 'warning', metric: 'calories' });
    });

    it('prioritizes carbohydrate excess over later checks', () => {
        const result = resolveNutritionInsight({ ...BASE_INPUT, totals: { ...BASE_INPUT.totals, carbs: 310, fiber: 5 } });

        expect(result).toMatchObject({ kind: 'carb-excess', metric: 'carbs' });
    });

    it('reports a protein deficit only after meaningful calorie progress', () => {
        const result = resolveNutritionInsight({ ...BASE_INPUT, totals: { ...BASE_INPUT.totals, proteins: 50 } });

        expect(result).toMatchObject({ kind: 'protein-deficit', metric: 'proteins' });
    });

    it('keeps an early day neutral instead of reporting a deficit', () => {
        const result = resolveNutritionInsight({
            ...BASE_INPUT,
            mealCount: 1,
            totals: { ...BASE_INPUT.totals, calories: 500, proteins: 20, fiber: 4 },
        });

        expect(result).toMatchObject({ kind: 'in-progress', tone: 'neutral', metric: 'calories' });
    });

    it('returns balanced only after enough of the day is represented', () => {
        expect(resolveNutritionInsight(BASE_INPUT)).toMatchObject({ kind: 'balanced', tone: 'positive' });
    });
});
describe('Dashboard snapshot nutrition insight', () => {
    it('stays neutral until a snapshot has loaded', () => {
        expect(resolveDashboardNutritionInsight(null)).toEqual({ kind: 'empty', tone: 'neutral' });
    });

    it('shows an excess warning at the fat threshold even when fiber also has a deficit', () => {
        const snapshot = createDashboardSnapshot({ averageFats: 92, averageFiber: 10 });

        expect(resolveDashboardNutritionInsight(snapshot)).toEqual({
            kind: 'fat-excess',
            tone: 'warning',
            metric: 'fats',
            current: 92,
            goal: 80,
        });
    });

    it('shows the fiber deficit once half of the calorie goal and the protein threshold are reached', () => {
        const snapshot = createDashboardSnapshot({ totalCalories: 1100, averageProteins: 72, averageFats: 91, averageFiber: 14 });

        expect(resolveDashboardNutritionInsight(snapshot)).toEqual({
            kind: 'fiber-deficit',
            tone: 'warning',
            metric: 'fiber',
            current: 14,
            goal: 30,
        });
    });

    it.each([null, undefined, 0])('omits a meaningful calorie comparison if the goal is %s', calorieGoal => {
        const insight = resolveNutritionInsight({ ...BASE_INPUT, goals: { ...BASE_INPUT.goals, calories: calorieGoal } });

        expect(insight).toEqual({ kind: 'in-progress', tone: 'neutral', metric: 'calories', current: BASE_INPUT.totals.calories });
    });
});

function createDashboardSnapshot(statistics: Partial<DashboardSnapshot['statistics']>): DashboardSnapshot {
    return {
        date: '2026-07-16',
        dateTo: '2026-07-16',
        dailyGoal: 2200,
        weeklyCalorieGoal: 15400,
        statistics: {
            totalCalories: BASE_INPUT.totals.calories,
            averageProteins: BASE_INPUT.totals.proteins,
            averageFats: BASE_INPUT.totals.fats,
            averageCarbs: BASE_INPUT.totals.carbs,
            averageFiber: BASE_INPUT.totals.fiber,
            proteinGoal: BASE_INPUT.goals.proteins,
            fatGoal: BASE_INPUT.goals.fats,
            carbGoal: BASE_INPUT.goals.carbs,
            fiberGoal: BASE_INPUT.goals.fiber,
            ...statistics,
        },
        weeklyCalories: [],
        weight: { latest: null, previous: null, desiredWeightKg: null },
        waist: { latest: null, previous: null, desiredWaistCm: null },
        meals: { items: [], total: BASE_INPUT.mealCount },
    };
}
