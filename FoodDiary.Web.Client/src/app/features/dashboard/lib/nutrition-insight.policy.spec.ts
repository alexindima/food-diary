import { describe, expect, it } from 'vitest';

import { type NutritionInsightInput, resolveNutritionInsight } from './nutrition-insight.policy';

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
