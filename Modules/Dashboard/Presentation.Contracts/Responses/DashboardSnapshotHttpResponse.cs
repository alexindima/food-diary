using FoodDiary.Modules.Hydration.Presentation.Contracts.Responses;
using FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;
using FoodDiary.Modules.Fasting.Presentation.Contracts.Responses;
using FoodDiary.Modules.Tdee.Presentation.Contracts.Responses;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WaistEntries.Responses;
using FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WeightEntries.Responses;

namespace FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;

public sealed record DashboardSnapshotHttpResponse(
    DateTime Date,
    DateTime DateTo,
    double DailyGoal,
    double WeeklyCalorieGoal,
    DashboardStatisticsHttpResponse Statistics,
    IReadOnlyList<DailyCaloriesHttpResponse> WeeklyCalories,
    DashboardWeightHttpResponse Weight,
    DashboardWaistHttpResponse Waist,
    DashboardMealsHttpResponse Meals,
    HydrationDailyHttpResponse? Hydration = null,
    DailyAdviceHttpResponse? Advice = null,
    FastingSessionHttpResponse? CurrentFastingSession = null,
    IReadOnlyList<WeightEntrySummaryHttpResponse>? WeightTrend = null,
    IReadOnlyList<WaistEntrySummaryHttpResponse>? WaistTrend = null,
    DashboardLayoutHttpModel? DashboardLayout = null,
    double CaloriesBurned = 0,
    TdeeInsightHttpResponse? TdeeInsight = null,
    CycleHttpResponse? CurrentCycle = null);
