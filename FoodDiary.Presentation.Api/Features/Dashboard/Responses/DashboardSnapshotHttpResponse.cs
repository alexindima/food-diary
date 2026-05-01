using FoodDiary.Presentation.Api.Features.Hydration.Responses;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;
using FoodDiary.Presentation.Api.Features.Tdee.Responses;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Features.WeightEntries.Responses;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Responses;

public sealed record DashboardSnapshotHttpResponse(
    DateTime Date,
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
