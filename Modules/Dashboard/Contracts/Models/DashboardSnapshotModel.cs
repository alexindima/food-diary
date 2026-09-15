using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Tdee.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

namespace FoodDiary.Modules.Dashboard.Contracts.Models;

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
