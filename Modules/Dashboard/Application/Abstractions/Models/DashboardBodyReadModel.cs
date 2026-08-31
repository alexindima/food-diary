namespace FoodDiary.Application.Abstractions.Dashboard.Models;

public sealed record DashboardBodyReadModel(
    IReadOnlyList<DashboardWeightPointReadModel> LatestWeightEntries,
    IReadOnlyList<DashboardWaistPointReadModel> LatestWaistEntries,
    IReadOnlyList<DashboardWeightSummaryReadModel> WeightTrend,
    IReadOnlyList<DashboardWaistSummaryReadModel> WaistTrend,
    int HydrationTotalMl);
