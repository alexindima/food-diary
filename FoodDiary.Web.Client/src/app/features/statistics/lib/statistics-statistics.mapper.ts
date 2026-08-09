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
    let breakfastCalories = 0;
    let lunchCalories = 0;
    let dinnerCalories = 0;
    let snackCalories = 0;
    let mealCount = 0;
    let trackedDayCount = 0;

    statistics.forEach(stat => {
        dates.push(new Date(stat.dateFrom));
        calories.push(stat.totalCalories);
        proteins.push(stat.averageProteins);
        fats.push(stat.averageFats);
        carbs.push(stat.averageCarbs);
        fiber.push(stat.averageFiber);

        totalProteins += stat.totalProteins;
        totalFats += stat.totalFats;
        totalCarbs += stat.totalCarbs;
        totalFiber += stat.totalFiber;
        breakfastCalories += stat.breakfastCalories ?? 0;
        lunchCalories += stat.lunchCalories ?? 0;
        dinnerCalories += stat.dinnerCalories ?? 0;
        snackCalories += stat.snackCalories ?? 0;
        mealCount += stat.mealCount ?? 0;
        trackedDayCount += stat.trackedDayCount ?? 0;
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
        mealStructure: {
            breakfastCalories,
            lunchCalories,
            dinnerCalories,
            snackCalories,
            mealCount,
            trackedDayCount,
        },
    };
}
