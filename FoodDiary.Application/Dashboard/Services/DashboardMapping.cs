using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Dashboard.Services;

public static class DashboardMapping {
    public static DashboardStatisticsModel ToStatisticsModel(AggregatedStatisticsModel? response, User? user) {
        if (response is null) {
            return new DashboardStatisticsModel(0, 0, 0, 0, 0, null, null, null, null);
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
            .OrderBy(r => r.DateFrom)
            .Select(r => new DailyCaloriesModel(r.DateFrom, r.TotalCalories))
            .ToList();
    }

    public static DashboardWeightModel ToWeightModel(IReadOnlyList<WeightEntry> entries, double? desired) {
        var latest = entries.FirstOrDefault();
        var previous = entries.Skip(1).FirstOrDefault();

        return new DashboardWeightModel(
            latest is null ? null : new WeightPointModel(latest.Date, latest.Weight),
            previous is null ? null : new WeightPointModel(previous.Date, previous.Weight),
            desired);
    }

    public static DashboardWaistModel ToWaistModel(IReadOnlyList<WaistEntry> entries, double? desired) {
        var latest = entries.FirstOrDefault();
        var previous = entries.Skip(1).FirstOrDefault();

        return new DashboardWaistModel(
            latest is null ? null : new WaistPointModel(latest.Date, latest.Circumference),
            previous is null ? null : new WaistPointModel(previous.Date, previous.Circumference),
            desired);
    }
}
