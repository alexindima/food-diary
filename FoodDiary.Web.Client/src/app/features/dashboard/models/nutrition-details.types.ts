import type { Meal } from '../../../shared/models/meal.data';

export const NUTRITION_METRICS = ['calories', 'protein', 'fats', 'carbs', 'fiber'] as const;
export type NutritionMetric = (typeof NUTRITION_METRICS)[number];

export type NutritionDetailsData = { date: Date; metric: NutritionMetric };
export type NutritionDetailsAction = { kind: 'meal'; meal: Meal } | { kind: 'goals' } | { kind: 'diary' };

export type NutritionSource = {
    id: string;
    meal: Meal;
    name: string | null;
    kind: 'meal' | 'product' | 'recipe' | 'ai' | 'manual' | 'unavailable';
    value: number;
};

export type NutritionSourceView = NutritionSource & { title: string; subtitle: string; share: number };

export function isNutritionMetric(value: string): value is NutritionMetric {
    const metrics: readonly string[] = NUTRITION_METRICS;
    return metrics.includes(value);
}
