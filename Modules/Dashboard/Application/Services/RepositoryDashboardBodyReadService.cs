using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals;
using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Application.Services;

internal sealed class RepositoryDashboardBodyReadService(
    ISender sender) : IDashboardBodyReadService {
    public async Task<DashboardBodyReadModel> GetBodyAsync(
        UserId userId,
        DateTime dayStart,
        DateTime dayEndStart,
        DateTime trendStart,
        int trendQuantizationDays,
        bool includeWeight,
        bool includeWaist,
        bool includeHydration,
        CancellationToken cancellationToken = default,
        DashboardCalendarRange? calendar = null) {
        int normalizedTrendQuantizationDays = Math.Max(1, trendQuantizationDays);
        IReadOnlyList<WeightEntryModel> latestWeightEntries = includeWeight
            ? await sender.Send(new ReadWeightEntriesQuery(UserId: userId, DateFrom: null, DateTo: calendar?.DateTo ?? dayEndStart, Limit: 2, Descending: true), cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<WaistEntryModel> latestWaistEntries = includeWaist
            ? await sender.Send(new ReadWaistEntriesQuery(UserId: userId, DateFrom: null, DateTo: calendar?.DateTo ?? dayEndStart, Limit: 2, Descending: true), cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<WeightEntrySummaryModel> weightTrend = includeWeight
            ? await sender.Send(new ReadWeightSummariesQuery(UserId: userId, DateFrom: calendar?.TrendDateFrom ?? trendStart, DateTo: calendar?.Date ?? dayStart, QuantizationDays: normalizedTrendQuantizationDays), cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<WaistEntrySummaryModel> waistTrend = includeWaist
            ? await sender.Send(new ReadWaistSummariesQuery(UserId: userId, DateFrom: calendar?.TrendDateFrom ?? trendStart, DateTo: calendar?.Date ?? dayStart, QuantizationDays: normalizedTrendQuantizationDays), cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<(DateTime Date, int TotalMl)> hydrationTotals = includeHydration
            ? await sender.Send(new ReadHydrationDailyTotalsQuery(userId, dayStart, dayEndStart, UseExactBounds: true), cancellationToken).ConfigureAwait(false)
            : [];

        return new DashboardBodyReadModel(
            [.. latestWeightEntries.Select(entry => new DashboardWeightPointReadModel(entry.Date, entry.WeightKg))],
            [.. latestWaistEntries.Select(entry => new DashboardWaistPointReadModel(entry.Date, entry.CircumferenceCm))],
            [.. weightTrend.Select(summary => new DashboardWeightSummaryReadModel(summary.StartDate, summary.EndDate, summary.AverageWeightKg))],
            [.. waistTrend.Select(summary => new DashboardWaistSummaryReadModel(summary.StartDate, summary.EndDate, summary.AverageCircumferenceCm))],
            hydrationTotals.Sum(total => total.TotalMl));
    }
}
