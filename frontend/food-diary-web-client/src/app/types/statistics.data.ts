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

export interface ChartData {
    type: ChartDataType;
    data: ReadonlyArray<[TuiDay, number]>;
}

export interface CaloriesChartData extends ChartData {
    maxValue: number;
}

export interface NutrientsChartData {
    chartData: ChartData[];
    maxValue: number;
}

export interface PieChartData {
    values: number[];
    labels: ChartDataType[];
}

export class StatisticsMapper {
    public static mapNutrientsToDaysChartData(statistics: AggregatedStatistics[]): NutrientsChartData {
        const proteins: Array<[TuiDay, number]> = [];
        const fats: Array<[TuiDay, number]> = [];
        const carbs: Array<[TuiDay, number]> = [];

        statistics.forEach(stat => {
            const day = TuiDay.fromLocalNativeDate(new Date(stat.dateFrom));
            proteins.push([day, stat.averageProteins]);
            fats.push([day, stat.averageFats]);
            carbs.push([day, stat.averageCarbs]);
        });

        const maxValue = Math.max(
            ...proteins.map(([, value]) => value),
            ...fats.map(([, value]) => value),
            ...carbs.map(([, value]) => value),
        );

        return {
            chartData: [
                { type: 'Proteins', data: proteins },
                { type: 'Fats', data: fats },
                { type: 'Carbs', data: carbs },
            ],
            maxValue,
        };
    }

    public static mapCaloriesToChartData(statistics: AggregatedStatistics[]): CaloriesChartData {
        const calories: Array<[TuiDay, number]> = [];

        statistics.forEach(stat => {
            const day = TuiDay.fromLocalNativeDate(new Date(stat.dateFrom));
            calories.push([day, stat.totalCalories]);
        });

        const maxValue = Math.max(...calories.map(([, value]) => value));

        return {
            type: 'Calories',
            data: calories,
            maxValue,
        };
    }

    public static mapNutrientsToPieChartData(statistics: AggregatedStatistics[]): PieChartData {
        let totalProteins = 0;
        let totalFats = 0;
        let totalCarbs = 0;

        statistics.forEach(stat => {
            totalProteins += stat.averageProteins;
            totalFats += stat.averageFats;
            totalCarbs += stat.averageCarbs;
        });

        return {
            values: [totalProteins, totalFats, totalCarbs],
            labels: ['Proteins', 'Fats', 'Carbs'],
        };
    }
}

export type ChartDataType = 'Calories' | 'Proteins' | 'Fats' | 'Carbs';
