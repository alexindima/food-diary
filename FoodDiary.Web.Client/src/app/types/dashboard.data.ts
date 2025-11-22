import { Consumption } from './consumption.data';

export interface DashboardSnapshot {
    date: string;
    dailyGoal: number;
    statistics: DashboardStatistics;
    weight: DashboardWeight;
    waist: DashboardWaist;
    meals: DashboardMeals;
}

export interface DashboardStatistics {
    totalCalories: number;
    averageProteins: number;
    averageFats: number;
    averageCarbs: number;
    averageFiber: number;
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
