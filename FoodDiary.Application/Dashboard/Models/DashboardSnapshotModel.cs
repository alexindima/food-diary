using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.Dashboard.Models;

public sealed record DashboardSnapshotModel(
    DateTime Date,
    double DailyGoal,
    double WeeklyCalorieGoal,
    DashboardStatisticsModel Statistics,
    IReadOnlyList<DailyCaloriesModel> WeeklyCalories,
    DashboardWeightModel Weight,
    DashboardWaistModel Waist,
    DashboardMealsModel Meals,
    HydrationDailyModel? Hydration = null,
    DailyAdviceModel? Advice = null,
    FastingSessionModel? CurrentFastingSession = null,
    IReadOnlyList<WeightEntrySummaryModel>? WeightTrend = null,
    IReadOnlyList<WaistEntrySummaryModel>? WaistTrend = null,
    DashboardLayoutModel? DashboardLayout = null,
    double CaloriesBurned = 0);
