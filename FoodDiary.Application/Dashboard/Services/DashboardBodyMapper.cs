using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Dashboard.Services;

internal static class DashboardBodyMapper {
    public static DashboardWeightModel ToWeightModel(IReadOnlyList<WeightEntry> entries, double? desired) {
        WeightEntry? latest = entries.Count > 0 ? entries[0] : null;
        WeightEntry? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWeightModel(
            latest is null ? null : new WeightPointModel(latest.Date, latest.Weight),
            previous is null ? null : new WeightPointModel(previous.Date, previous.Weight),
            desired);
    }

    public static DashboardWeightModel ToWeightModel(IReadOnlyList<DashboardWeightPointReadModel> entries, double? desired) {
        DashboardWeightPointReadModel? latest = entries.Count > 0 ? entries[0] : null;
        DashboardWeightPointReadModel? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWeightModel(
            latest is null ? null : new WeightPointModel(latest.Date, latest.Weight),
            previous is null ? null : new WeightPointModel(previous.Date, previous.Weight),
            desired);
    }

    public static DashboardWaistModel ToWaistModel(IReadOnlyList<WaistEntry> entries, double? desired) {
        WaistEntry? latest = entries.Count > 0 ? entries[0] : null;
        WaistEntry? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWaistModel(
            latest is null ? null : new WaistPointModel(latest.Date, latest.Circumference),
            previous is null ? null : new WaistPointModel(previous.Date, previous.Circumference),
            desired);
    }

    public static DashboardWaistModel ToWaistModel(IReadOnlyList<DashboardWaistPointReadModel> entries, double? desired) {
        DashboardWaistPointReadModel? latest = entries.Count > 0 ? entries[0] : null;
        DashboardWaistPointReadModel? previous = entries.Count > 1 ? entries[1] : null;

        return new DashboardWaistModel(
            latest is null ? null : new WaistPointModel(latest.Date, latest.Circumference),
            previous is null ? null : new WaistPointModel(previous.Date, previous.Circumference),
            desired);
    }

    public static IReadOnlyList<WeightEntrySummaryModel> ToWeightTrend(IReadOnlyList<DashboardWeightSummaryReadModel> responses) {
        return responses
            .OrderBy(response => response.DateFrom)
            .Select(response => new WeightEntrySummaryModel(response.DateFrom, response.DateTo, response.AverageWeight))
            .ToList();
    }

    public static IReadOnlyList<WaistEntrySummaryModel> ToWaistTrend(IReadOnlyList<DashboardWaistSummaryReadModel> responses) {
        return responses
            .OrderBy(response => response.DateFrom)
            .Select(response => new WaistEntrySummaryModel(response.DateFrom, response.DateTo, response.AverageCircumference))
            .ToList();
    }
}
