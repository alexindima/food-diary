import type { DashboardLayoutSettings } from '../../../shared/models/user.data';
import type { CycleResponse } from '../../cycle-tracking/models/cycle.data';
import type { FastingSession } from '../../fasting/models/fasting.data';
import type { HydrationDaily } from '../../hydration/models/hydration.data';
import type { Meal } from '../../meals/models/meal.data';
import type { WaistEntrySummaryPoint } from '../../waist-history/models/waist-entry.data';
import type { WeightEntrySummaryPoint } from '../../weight-history/models/weight-entry.data';
import type { DailyAdvice } from './daily-advice.data';
import type { TdeeInsight } from './tdee-insight.data';

export type DashboardSnapshot = {
    date: string;
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
};

export type DashboardWeight = {
    latest: WeightEntrySummary | null;
    previous: WeightEntrySummary | null;
    desired: number | null;
};

export type DashboardWaist = {
    latest: WaistEntrySummary | null;
    previous: WaistEntrySummary | null;
    desired: number | null;
};

export type DashboardMeals = {
    items: Meal[];
    total: number;
};

export type WeightEntrySummary = {
    date: string;
    weight: number;
};

export type WaistEntrySummary = {
    date: string;
    circumference: number;
};
