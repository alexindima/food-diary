import { TuiDay } from '@taiga-ui/cdk';

export interface AggregatedStatistics {
    dateFrom: Date;
    dateTo: Date;
    totalCalories: number;
    averageProteins: number;
    averageFats: number;
    averageCarbs: number;
    averageFiber: number;
}

export interface GetStatisticsDto {
    dateFrom: Date;
    dateTo: Date;
    quantizationDays?: number;
}

export interface MappedStatistics {
    date: TuiDay[];
    calories: number[];
    nutrientsStatistic: NutrientsStatistics;
    aggregatedNutrients: AggregatedNutrients;
}

export interface NutrientsStatistics {
    proteins: number[];
    fats: number[];
    carbs: number[];
    fiber: number[];
}

export interface AggregatedNutrients {
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
}

export class StatisticsMapper {
    public static mapStatistics(statistics: AggregatedStatistics[]): MappedStatistics {
        const dates: TuiDay[] = [];
        const calories: number[] = [];
        const proteins: number[] = [];
        const fats: number[] = [];
        const carbs: number[] = [];
        const fiber: number[] = [];
        let totalProteins = 0;
        let totalFats = 0;
        let totalCarbs = 0;
        let totalFiber = 0;

        statistics.forEach(stat => {
            const day = TuiDay.fromLocalNativeDate(new Date(stat.dateFrom));
            dates.push(day);

            calories.push(stat.totalCalories);

            proteins.push(stat.averageProteins);
            fats.push(stat.averageFats);
            carbs.push(stat.averageCarbs);
            fiber.push(stat.averageFiber);

            totalProteins += stat.averageProteins;
            totalFats += stat.averageFats;
            totalCarbs += stat.averageCarbs;
            totalFiber += stat.averageFiber;
        });

        return {
            date: dates,
            calories,
            nutrientsStatistic: {
                proteins,
                fats,
                carbs,
                fiber,
            },
            aggregatedNutrients: {
                proteins: totalProteins,
                fats: totalFats,
                carbs: totalCarbs,
                fiber: totalFiber,
            },
        };
    }
}
