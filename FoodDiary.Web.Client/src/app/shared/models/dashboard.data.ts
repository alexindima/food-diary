import type { CycleResponse } from './cycle.data';
import type { DailyAdvice } from './daily-advice.data';
import type { FastingSession } from './fasting.data';
import type { HydrationDaily } from './hydration.data';
import type { Meal } from './meal.data';
import type { TdeeInsight } from './tdee-insight.data';
import type { DashboardLayoutSettings } from './user.data';
import type { WaistEntrySummaryPoint } from './waist-entry.data';
import type { WeightEntrySummaryPoint } from './weight-entry.data';

export type DashboardSnapshot = {
    date: string;
    dateTo: string;
    dailyGoal: number;
    weeklyCalorieGoal: number;
    statistics: DashboardStatistics;
    weeklyCalories: WeeklyCaloriesPoint[];
    weight: DashboardWeight;
    waist: DashboardWaist;
    meals: DashboardMeals;
    hydration?: HydrationDaily | null;
    advice?: DailyAdvice | null;
    currentFastingSession?: FastingSession | null;
    weightTrend?: WeightEntrySummaryPoint[];
    waistTrend?: WaistEntrySummaryPoint[];
    dashboardLayout?: DashboardLayoutSettings | null;
    caloriesBurned?: number;
    tdeeInsight?: TdeeInsight | null;
    currentCycle?: CycleResponse | null;
};

export type DashboardStatistics = {
    totalCalories: number;
    averageProteins: number;
    averageFats: number;
    averageCarbs: number;
    averageFiber: number;
    proteinGoal?: number | null;
    fatGoal?: number | null;
    carbGoal?: number | null;
    fiberGoal?: number | null;
};

export type WeeklyCaloriesPoint = {
    date: string;
    calories: number;
    proteins?: number;
    fats?: number;
    carbs?: number;
    fiber?: number;
};

export type DashboardWeight = {
    latest: WeightEntrySummary | null;
    previous: WeightEntrySummary | null;
    desiredWeightKg: number | null;
};

export type DashboardWaist = {
    latest: WaistEntrySummary | null;
    previous: WaistEntrySummary | null;
    desiredWaistCm: number | null;
};

export type DashboardMeals = {
    items: Meal[];
    total: number;
};

export type WeightEntrySummary = {
    date: string;
    weightKg: number;
};

export type WaistEntrySummary = {
    date: string;
    circumferenceCm: number;
};
