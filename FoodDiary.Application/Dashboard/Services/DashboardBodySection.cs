using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed record DashboardBodySection(
    DashboardWeightModel Weight,
    DashboardWaistModel Waist,
    IReadOnlyList<WeightEntrySummaryModel> WeightTrend,
    IReadOnlyList<WaistEntrySummaryModel> WaistTrend,
    HydrationDailyModel? Hydration);
