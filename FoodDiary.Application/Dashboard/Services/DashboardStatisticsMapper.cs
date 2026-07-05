using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Statistics.Models;

namespace FoodDiary.Application.Dashboard.Services;

internal static class DashboardStatisticsMapper {
    public static DashboardStatisticsModel ToModel(DashboardStatisticsBucketReadModel? response, DashboardUserContextModel? user) {
        if (response is null) {
            return CreateEmpty();
        }

        return new DashboardStatisticsModel(
            response.TotalCalories,
            response.AverageProteins,
            response.AverageFats,
            response.AverageCarbs,
            response.AverageFiber,
            user?.ProteinTarget,
            user?.FatTarget,
            user?.CarbTarget,
            user?.FiberTarget);
    }

    public static DashboardStatisticsModel ToModel(AggregatedStatisticsModel? response, DashboardUserContextModel? user) {
        if (response is null) {
            return CreateEmpty();
        }

        return new DashboardStatisticsModel(
            response.TotalCalories,
            response.AverageProteins,
            response.AverageFats,
            response.AverageCarbs,
            response.AverageFiber,
            user?.ProteinTarget,
            user?.FatTarget,
            user?.CarbTarget,
            user?.FiberTarget);
    }

    public static IReadOnlyList<DailyCaloriesModel> ToWeeklyCalories(IReadOnlyList<AggregatedStatisticsModel> responses) {
        return responses
            .OrderBy(response => response.DateFrom)
            .Select(response => new DailyCaloriesModel(response.DateFrom, response.TotalCalories))
            .ToList();
    }

    public static IReadOnlyList<DailyCaloriesModel> ToWeeklyCalories(IReadOnlyList<DashboardStatisticsBucketReadModel> responses) {
        return responses
            .OrderBy(response => response.DateFrom)
            .Select(response => new DailyCaloriesModel(response.DateFrom, response.TotalCalories))
            .ToList();
    }

    private static DashboardStatisticsModel CreateEmpty() =>
        new(0, 0, 0, 0, 0, ProteinGoal: null, FatGoal: null, CarbGoal: null, FiberGoal: null);
}
