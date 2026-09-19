using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Statistics.Application.Models;

namespace FoodDiary.Modules.Statistics.Application.Mappings;

internal static class StatisticsMappings {
    internal static AggregatedStatisticsModel ToModel(MealNutritionStatisticsBucket model) =>
        new(
            model.DateFrom,
            model.DateTo,
            model.TotalCalories,
            model.AverageProteins,
            model.AverageFats,
            model.AverageCarbs,
            model.AverageFiber,
            model.TotalProteins,
            model.TotalFats,
            model.TotalCarbs,
            model.TotalFiber,
            model.BreakfastCalories,
            model.LunchCalories,
            model.DinnerCalories,
            model.SnackCalories,
            model.MealCount,
            model.TrackedDayCount);
}
