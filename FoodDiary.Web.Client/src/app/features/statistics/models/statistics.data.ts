export type AggregatedStatistics = {
    dateFrom: Date;
    dateTo: Date;
    totalCalories: number;
    averageProteins: number;
    averageFats: number;
    averageCarbs: number;
    averageFiber: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
    totalFiber: number;
    breakfastCalories?: number;
    lunchCalories?: number;
    dinnerCalories?: number;
    snackCalories?: number;
    otherCalories?: number;
    mealCount?: number;
    trackedDayCount?: number;
};

export type GetStatisticsDto = {
    dateFrom: Date | string;
    dateTo: Date | string;
    quantizationDays?: number;
    timeZoneId?: string;
};

export type GetStatisticsSummaryDto = GetStatisticsDto & {
    bodyDateFrom?: string;
    bodyDateTo?: string;
};

export type StatisticsSummary = {
    nutrition: AggregatedStatistics[];
    weight: WeightEntrySummaryPoint[];
    waist: WaistEntrySummaryPoint[];
};

export type MappedStatistics = {
    date: Date[];
    calories: number[];
    nutrientsStatistic: NutrientsStatistics;
    aggregatedNutrients: AggregatedNutrients;
    mealStructure?: MealStructureTotals;
};

export type MealStructureTotals = {
    breakfastCalories: number;
    lunchCalories: number;
    dinnerCalories: number;
    snackCalories: number;
    otherCalories?: number;
    mealCount: number;
    trackedDayCount: number;
};

export type NutrientsStatistics = {
    proteins: number[];
    fats: number[];
    carbs: number[];
    fiber: number[];
};

export type AggregatedNutrients = {
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
};
import type { WaistEntrySummaryPoint } from '../../../shared/models/waist-entry.data';
import type { WeightEntrySummaryPoint } from '../../../shared/models/weight-entry.data';
