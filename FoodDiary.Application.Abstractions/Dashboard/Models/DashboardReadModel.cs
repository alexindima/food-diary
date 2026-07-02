namespace FoodDiary.Application.Abstractions.Dashboard.Models;

public sealed record DashboardReadModel(
    IReadOnlyList<DashboardStatisticsBucketReadModel> Statistics,
    IReadOnlyList<DashboardStatisticsBucketReadModel> WeeklyStatistics,
    DashboardBodyReadModel Body,
    DashboardMealsReadModel Meals);
