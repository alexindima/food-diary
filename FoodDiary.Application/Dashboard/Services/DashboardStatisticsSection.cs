using FoodDiary.Application.Dashboard.Models;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed record DashboardStatisticsSection(
    DashboardStatisticsModel Statistics,
    IReadOnlyList<DailyCaloriesModel> WeeklyCalories);
