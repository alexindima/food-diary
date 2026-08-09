import type { FdUiLineChartPoint } from 'fd-ui-kit';

export type StatisticsNutrientKey = 'protein' | 'fat' | 'carbs' | 'fiber';

export type StatisticsNutrientProgress = {
    key: StatisticsNutrientKey;
    current: number;
    goal: number;
};

export type StatisticsOverviewData = {
    daysWithinGoal: number;
    trackedDays: number;
    periodDays: number;
    averageCalories: number;
    calorieGoal: number;
    calorieChangePercent: number | null;
    nutrients: readonly StatisticsNutrientProgress[];
};

export type StatisticsNutritionDay = {
    date: string;
    label: string;
    calories: number | null;
    protein: number;
    fat: number;
    carbs: number;
    fiber: number;
};

export type StatisticsTrendInsight = {
    key: string;
    labelKey: string;
    value: number | null;
    unitKey: string;
    tone: 'positive' | 'negative' | 'neutral';
    detailKey: string;
};

export type StatisticsNutrientBalanceItem = StatisticsNutrientProgress;

export type StatisticsBodyTrendData = {
    currentWeight: number | null;
    change: number | null;
    timeframeDays: number;
    points: readonly FdUiLineChartPoint[];
};
