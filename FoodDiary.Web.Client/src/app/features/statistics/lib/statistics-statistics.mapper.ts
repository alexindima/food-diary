import type { AggregatedStatistics, MappedStatistics } from '../models/statistics.data';

export function mapStatistics(statistics: AggregatedStatistics[]): MappedStatistics {
    const dates: Date[] = [];
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
        dates.push(new Date(stat.dateFrom));
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
