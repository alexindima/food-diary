import { Consumption } from './consumption.data';
import { DailyAdvice } from './daily-advice.data';
import { HydrationDaily } from './hydration.data';
import { WaistEntrySummaryPoint } from './waist-entry.data';
import { WeightEntrySummaryPoint } from './weight-entry.data';

export interface DashboardSnapshot {
    date: string;
    dailyGoal: number;
    statistics: DashboardStatistics;
    weeklyCalories: WeeklyCaloriesPoint[];
    weight: DashboardWeight;
    waist: DashboardWaist;
    meals: DashboardMeals;
    hydration?: HydrationDaily | null;
    advice?: DailyAdvice | null;
    weightTrend?: WeightEntrySummaryPoint[];
    waistTrend?: WaistEntrySummaryPoint[];
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
    items: Consumption[];
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
