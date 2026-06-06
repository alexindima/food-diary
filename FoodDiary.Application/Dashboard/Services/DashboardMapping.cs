using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Dashboard.Services;

public static class DashboardMapping {
    public static DashboardStatisticsModel ToStatisticsModel(AggregatedStatisticsModel? response, User? user) {
        if (response is null) {
            return new DashboardStatisticsModel(0, 0, 0, 0, 0, ProteinGoal: null, FatGoal: null, CarbGoal: null, FiberGoal: null);
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
        WeightEntry? latest = entries.Count > 0 ? entries[0] : null;
        WeightEntry? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWeightModel(
            latest is null ? null : new WeightPointModel(latest.Date, latest.Weight),
            previous is null ? null : new WeightPointModel(previous.Date, previous.Weight),
            desired);
    }

    public static DashboardWaistModel ToWaistModel(IReadOnlyList<WaistEntry> entries, double? desired) {
        WaistEntry? latest = entries.Count > 0 ? entries[0] : null;
        WaistEntry? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWaistModel(
            latest is null ? null : new WaistPointModel(latest.Date, latest.Circumference),
            previous is null ? null : new WaistPointModel(previous.Date, previous.Circumference),
            desired);
    }
}
