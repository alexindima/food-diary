using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Application.Abstractions.WeightEntries.Models;

namespace FoodDiary.Application.Dashboard.Models;

public sealed record DashboardSnapshotModel(
    DateTime Date,
    DateTime DateTo,
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
    double CaloriesBurned = 0,
    TdeeInsightModel? TdeeInsight = null,
    CycleModel? CurrentCycle = null);
