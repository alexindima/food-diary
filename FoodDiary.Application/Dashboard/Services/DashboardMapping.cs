using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Dashboard.Services;

public static class DashboardMapping {
    public static DashboardStatisticsModel ToStatisticsModel(DashboardStatisticsBucketReadModel? response, User? user) =>
        DashboardStatisticsMapper.ToModel(response, user);

    public static DashboardStatisticsModel ToStatisticsModel(AggregatedStatisticsModel? response, User? user) =>
        DashboardStatisticsMapper.ToModel(response, user);

    public static IReadOnlyList<DailyCaloriesModel> ToWeeklyCalories(IReadOnlyList<AggregatedStatisticsModel> responses) =>
        DashboardStatisticsMapper.ToWeeklyCalories(responses);

    public static IReadOnlyList<DailyCaloriesModel> ToWeeklyCalories(IReadOnlyList<DashboardStatisticsBucketReadModel> responses) =>
        DashboardStatisticsMapper.ToWeeklyCalories(responses);

    public static DashboardWeightModel ToWeightModel(IReadOnlyList<WeightEntry> entries, double? desired) =>
        DashboardBodyMapper.ToWeightModel(entries, desired);

    public static DashboardWeightModel ToWeightModel(IReadOnlyList<DashboardWeightPointReadModel> entries, double? desired) =>
        DashboardBodyMapper.ToWeightModel(entries, desired);

    public static DashboardWaistModel ToWaistModel(IReadOnlyList<WaistEntry> entries, double? desired) =>
        DashboardBodyMapper.ToWaistModel(entries, desired);

    public static DashboardWaistModel ToWaistModel(IReadOnlyList<DashboardWaistPointReadModel> entries, double? desired) =>
        DashboardBodyMapper.ToWaistModel(entries, desired);

    public static IReadOnlyList<WeightEntrySummaryModel> ToWeightTrend(IReadOnlyList<DashboardWeightSummaryReadModel> responses) =>
        DashboardBodyMapper.ToWeightTrend(responses);

    public static IReadOnlyList<WaistEntrySummaryModel> ToWaistTrend(IReadOnlyList<DashboardWaistSummaryReadModel> responses) =>
        DashboardBodyMapper.ToWaistTrend(responses);

    public static DashboardMealsModel ToMealsModel(DashboardMealsReadModel response) =>
        DashboardMealsMapper.ToModel(response);
}
