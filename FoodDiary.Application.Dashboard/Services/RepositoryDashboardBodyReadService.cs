using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class RepositoryDashboardBodyReadService(
    IWeightEntryReadService weightEntryReadService,
    IWaistEntryReadService waistEntryReadService,
    IHydrationEntryReadService hydrationEntryReadService) : IDashboardBodyReadService {
    public async Task<DashboardBodyReadModel> GetBodyAsync(
        UserId userId,
        DateTime dayStart,
        DateTime dayEndStart,
        DateTime trendStart,
        int trendQuantizationDays,
        bool includeWeight,
        bool includeWaist,
        bool includeHydration,
        CancellationToken cancellationToken = default) {
        int normalizedTrendQuantizationDays = Math.Max(1, trendQuantizationDays);
        IReadOnlyList<WeightEntryModel> latestWeightEntries = includeWeight
            ? await weightEntryReadService.GetEntriesAsync(
                userId, dateFrom: null, dayEndStart, 2, descending: true, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<WaistEntryModel> latestWaistEntries = includeWaist
            ? await waistEntryReadService.GetEntriesAsync(
                userId, dateFrom: null, dayEndStart, 2, descending: true, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<WeightEntrySummaryModel> weightTrend = includeWeight
            ? await weightEntryReadService.GetSummariesAsync(
                userId, trendStart, dayStart, normalizedTrendQuantizationDays, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<WaistEntrySummaryModel> waistTrend = includeWaist
            ? await waistEntryReadService.GetSummariesAsync(
                userId, trendStart, dayStart, normalizedTrendQuantizationDays, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<(DateTime Date, int TotalMl)> hydrationTotals = includeHydration
            ? await hydrationEntryReadService.GetDailyTotalsAsync(
                userId, dayStart, dayEndStart, cancellationToken).ConfigureAwait(false)
            : [];

        return new DashboardBodyReadModel(
            [.. latestWeightEntries.Select(entry => new DashboardWeightPointReadModel(entry.Date, entry.Weight))],
            [.. latestWaistEntries.Select(entry => new DashboardWaistPointReadModel(entry.Date, entry.Circumference))],
            [.. weightTrend.Select(summary => new DashboardWeightSummaryReadModel(summary.StartDate, summary.EndDate, summary.AverageWeight))],
            [.. waistTrend.Select(summary => new DashboardWaistSummaryReadModel(summary.StartDate, summary.EndDate, summary.AverageCircumference))],
            hydrationTotals.Sum(total => total.TotalMl));
    }
}
