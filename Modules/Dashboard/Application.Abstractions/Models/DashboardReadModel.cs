using FoodDiary.Modules.Dashboard.Contracts.Models;
namespace FoodDiary.Modules.Dashboard.Application.Abstractions.Models;

public sealed record DashboardReadModel(
    IReadOnlyList<DashboardStatisticsBucketReadModel> Statistics,
    IReadOnlyList<DashboardStatisticsBucketReadModel> WeeklyStatistics,
    DashboardBodyReadModel Body,
    DashboardMealsReadModel Meals);
