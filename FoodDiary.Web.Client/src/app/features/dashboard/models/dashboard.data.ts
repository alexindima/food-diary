import { DashboardLayoutSettings } from '../../../shared/models/user.data';
import { FastingSession } from '../../fasting/models/fasting.data';
import { HydrationDaily } from '../../hydration/models/hydration.data';
import { Meal } from '../../meals/models/meal.data';
import { WaistEntrySummaryPoint } from '../../waist-history/models/waist-entry.data';
import { WeightEntrySummaryPoint } from '../../weight-history/models/weight-entry.data';
import { DailyAdvice } from './daily-advice.data';

export interface DashboardSnapshot {
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
}

export interface DashboardStatistics {
    totalCalories: number;
    averageProteins: number;
    averageFats: number;
    averageCarbs: number;
    averageFiber: number;
    proteinGoal?: number | null;
    fatGoal?: number | null;
    carbGoal?: number | null;
    fiberGoal?: number | null;
}

export interface WeeklyCaloriesPoint {
    date: string;
    calories: number;
}

export interface DashboardWeight {
    latest: WeightEntrySummary | null;
    previous: WeightEntrySummary | null;
    desired: number | null;
}

export interface DashboardWaist {
    latest: WaistEntrySummary | null;
    previous: WaistEntrySummary | null;
    desired: number | null;
}

export interface DashboardMeals {
    items: Meal[];
    total: number;
}

export interface WeightEntrySummary {
    date: string;
    weight: number;
}

export interface WaistEntrySummary {
    date: string;
    circumference: number;
}
