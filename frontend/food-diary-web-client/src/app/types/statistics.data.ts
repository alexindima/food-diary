import { TuiDay } from '@taiga-ui/cdk';

export interface AggregatedStatistics {
    dateFrom: Date;
    dateTo: Date;
    totalCalories: number;
    averageProteins: number;
    averageFats: number;
    averageCarbs: number;
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
}

export interface AggregatedNutrients {
    proteins: number;
    fats: number;
    carbs: number;
}

export class StatisticsMapper {
    public static mapStatistics(statistics: AggregatedStatistics[]): MappedStatistics {
        const dates: TuiDay[] = [];
        const calories: number[] = [];
        const proteins: number[] = [];
        const fats: number[] = [];
        const carbs: number[] = [];
        let totalProteins = 0;
        let totalFats = 0;
        let totalCarbs = 0;

        statistics.forEach(stat => {
            const day = TuiDay.fromLocalNativeDate(new Date(stat.dateFrom));
            dates.push(day);

            calories.push(stat.totalCalories);

            proteins.push(stat.averageProteins);
            fats.push(stat.averageFats);
            carbs.push(stat.averageCarbs);

            totalProteins += stat.averageProteins;
            totalFats += stat.averageFats;
            totalCarbs += stat.averageCarbs;
        });

        return {
            date: dates,
            calories,
            nutrientsStatistic: {
                proteins,
                fats,
                carbs,
            },
            aggregatedNutrients: {
                proteins: totalProteins,
                fats: totalFats,
                carbs: totalCarbs,
            },
        };
    }
}
