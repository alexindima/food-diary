export type NutritionInsightKind =
    'empty' | 'calorie-excess' | 'carb-excess' | 'fat-excess' | 'protein-deficit' | 'fiber-deficit' | 'in-progress' | 'balanced';

export type NutritionInsightTone = 'neutral' | 'positive' | 'warning';

export type NutritionInsightMetric = 'calories' | 'proteins' | 'fats' | 'carbs' | 'fiber';

export type NutritionInsight = {
    kind: NutritionInsightKind;
    tone: NutritionInsightTone;
    metric?: NutritionInsightMetric;
    current?: number;
    goal?: number;
};

export type NutritionInsightInput = {
    mealCount: number;
    totals: Record<NutritionInsightMetric, number>;
    goals: Record<NutritionInsightMetric, number | null | undefined>;
};

const CALORIE_EXCESS_RATIO = 1.1;
const NUTRIENT_EXCESS_RATIO = 1.15;
const DEFICIT_EVALUATION_CALORIE_RATIO = 0.5;
const BALANCED_EVALUATION_CALORIE_RATIO = 0.7;
const PROTEIN_DEFICIT_RATIO = 0.6;
const FIBER_DEFICIT_RATIO = 0.5;

export function resolveDashboardNutritionInsight(snapshot: DashboardSnapshot | null): NutritionInsight {
    if (snapshot === null) {
        return { kind: 'empty', tone: 'neutral' };
    }

    const statistics = snapshot.statistics;
    return resolveNutritionInsight({
        mealCount: snapshot.meals.total,
        totals: {
            calories: statistics.totalCalories,
            proteins: statistics.averageProteins,
            fats: statistics.averageFats,
            carbs: statistics.averageCarbs,
            fiber: statistics.averageFiber,
        },
        goals: {
            calories: snapshot.dailyGoal,
            proteins: statistics.proteinGoal,
            fats: statistics.fatGoal,
            carbs: statistics.carbGoal,
            fiber: statistics.fiberGoal,
        },
    });
}

export function resolveNutritionInsight(input: NutritionInsightInput): NutritionInsight {
    if (input.mealCount <= 0) {
        return { kind: 'empty', tone: 'neutral' };
    }

    const calorieProgress = ratio(input.totals.calories, input.goals.calories);
    const calorieExcess = excessInsight(input, 'calories', CALORIE_EXCESS_RATIO, 'calorie-excess');
    if (calorieExcess !== null) {
        return calorieExcess;
    }

    const carbExcess = excessInsight(input, 'carbs', NUTRIENT_EXCESS_RATIO, 'carb-excess');
    if (carbExcess !== null) {
        return carbExcess;
    }

    const fatExcess = excessInsight(input, 'fats', NUTRIENT_EXCESS_RATIO, 'fat-excess');
    if (fatExcess !== null) {
        return fatExcess;
    }

    if (calorieProgress >= DEFICIT_EVALUATION_CALORIE_RATIO) {
        const proteinDeficit = deficitInsight(input, 'proteins', PROTEIN_DEFICIT_RATIO, 'protein-deficit');
        if (proteinDeficit !== null) {
            return proteinDeficit;
        }

        const fiberDeficit = deficitInsight(input, 'fiber', FIBER_DEFICIT_RATIO, 'fiber-deficit');
        if (fiberDeficit !== null) {
            return fiberDeficit;
        }
    }

    if (calorieProgress < BALANCED_EVALUATION_CALORIE_RATIO) {
        return metricInsight(input, 'in-progress', 'neutral', 'calories');
    }

    return metricInsight(input, 'balanced', 'positive', 'calories');
}

function excessInsight(
    input: NutritionInsightInput,
    metric: NutritionInsightMetric,
    threshold: number,
    kind: NutritionInsightKind,
): NutritionInsight | null {
    return ratio(input.totals[metric], input.goals[metric]) >= threshold ? metricInsight(input, kind, 'warning', metric) : null;
}

function deficitInsight(
    input: NutritionInsightInput,
    metric: NutritionInsightMetric,
    threshold: number,
    kind: NutritionInsightKind,
): NutritionInsight | null {
    const goal = input.goals[metric];
    return goal !== null && goal !== undefined && goal > 0 && ratio(input.totals[metric], goal) < threshold
        ? metricInsight(input, kind, 'warning', metric)
        : null;
}

function metricInsight(
    input: NutritionInsightInput,
    kind: NutritionInsightKind,
    tone: NutritionInsightTone,
    metric: NutritionInsightMetric,
): NutritionInsight {
    const goal = input.goals[metric];
    return {
        kind,
        tone,
        metric,
        current: input.totals[metric],
        ...(goal !== null && goal !== undefined && goal > 0 ? { goal } : {}),
    };
}

function ratio(current: number, goal: number | null | undefined): number {
    return goal !== null && goal !== undefined && goal > 0 ? current / goal : 0;
}
import type { DashboardSnapshot } from '../models/dashboard.data';
