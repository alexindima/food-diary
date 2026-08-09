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

export type StatisticsMealType = 'breakfast' | 'lunch' | 'dinner' | 'snack';

export type StatisticsMealStructureItem = {
    key: StatisticsMealType;
    calories: number;
    percentage: number;
};

export type StatisticsMealStructureData = {
    totalCalories: number;
    averageMealsPerDay: number;
    dominantMeal: StatisticsMealType | null;
    items: readonly StatisticsMealStructureItem[];
};

export type StatisticsDietStabilityStatus = 'stable' | 'deviation' | 'missing';

export type StatisticsDietStabilityDay = {
    label: string;
    status: StatisticsDietStabilityStatus;
};

export type StatisticsDietStabilityData = {
    stableCount: number;
    totalCount: number;
    averageDeviationPercent: number | null;
    longestLoggingStreak: number;
    usesDailyIntervals: boolean;
    hasGoal: boolean;
    days: readonly StatisticsDietStabilityDay[];
};

export type StatisticsBodyMetricKey = 'weight' | 'waist';

export type StatisticsBodyMetricData = {
    key: StatisticsBodyMetricKey;
    current: number | null;
    change: number | null;
    goal: number | null;
    timeframeDays: number;
    points: readonly FdUiLineChartPoint[];
};

export type StatisticsBodyTrendData = {
    weight: StatisticsBodyMetricData;
    waist: StatisticsBodyMetricData;
};
