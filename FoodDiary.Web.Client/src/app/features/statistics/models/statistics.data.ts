export type AggregatedStatistics = {
    dateFrom: Date;
    dateTo: Date;
    totalCalories: number;
    averageProteins: number;
    averageFats: number;
    averageCarbs: number;
    averageFiber: number;
};

export type GetStatisticsDto = {
    dateFrom: Date | string;
    dateTo: Date | string;
    quantizationDays?: number;
};

export type MappedStatistics = {
    date: Date[];
    calories: number[];
    nutrientsStatistic: NutrientsStatistics;
    aggregatedNutrients: AggregatedNutrients;
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
