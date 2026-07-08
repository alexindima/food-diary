using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class RepositoryDashboardBodyReadService(
    IWeightEntryReadModelRepository weightEntryRepository,
    IWaistEntryReadModelRepository waistEntryRepository,
    IHydrationEntryReadModelRepository hydrationEntryRepository) : IDashboardBodyReadService {
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
        IReadOnlyList<WeightEntryReadModel> latestWeightEntries = includeWeight
            ? await weightEntryRepository.GetEntryReadModelsAsync(
                userId, dateFrom: null, dayEndStart, 2, descending: true, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<WaistEntryReadModel> latestWaistEntries = includeWaist
            ? await waistEntryRepository.GetEntryReadModelsAsync(
                userId, dateFrom: null, dayEndStart, 2, descending: true, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<WeightEntryReadModel> weightTrendEntries = includeWeight
            ? await weightEntryRepository.GetByPeriodReadModelsAsync(
                userId, trendStart, dayStart, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<WaistEntryReadModel> waistTrendEntries = includeWaist
            ? await waistEntryRepository.GetByPeriodReadModelsAsync(
                userId, trendStart, dayStart, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<(DateTime Date, int TotalMl)> hydrationTotals = includeHydration
            ? await hydrationEntryRepository.GetDailyTotalsAsync(
                userId, dayStart, dayEndStart, cancellationToken).ConfigureAwait(false)
            : [];

        return new DashboardBodyReadModel(
            [.. latestWeightEntries.Select(entry => new DashboardWeightPointReadModel(entry.Date, entry.Weight))],
            [.. latestWaistEntries.Select(entry => new DashboardWaistPointReadModel(entry.Date, entry.Circumference))],
            BuildWeightTrend(trendStart, dayStart, normalizedTrendQuantizationDays, weightTrendEntries),
            BuildWaistTrend(trendStart, dayStart, normalizedTrendQuantizationDays, waistTrendEntries),
            hydrationTotals.Sum(total => total.TotalMl));
    }

    private static IReadOnlyList<DashboardWeightSummaryReadModel> BuildWeightTrend(
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        IReadOnlyList<WeightEntryReadModel> entries) =>
        [.. BuildBuckets(dateFrom, dateTo, quantizationDays)
            .Select(bucket => BuildWeightSummary(bucket.Start, bucket.End, entries))];

    private static IReadOnlyList<DashboardWaistSummaryReadModel> BuildWaistTrend(
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        IReadOnlyList<WaistEntryReadModel> entries) =>
        [.. BuildBuckets(dateFrom, dateTo, quantizationDays)
            .Select(bucket => BuildWaistSummary(bucket.Start, bucket.End, entries))];

    private static DashboardWeightSummaryReadModel BuildWeightSummary(
        DateTime start,
        DateTime end,
        IReadOnlyList<WeightEntryReadModel> entries) {
        WeightEntryReadModel[] bucketEntries = [.. entries.Where(entry => entry.Date >= start && entry.Date <= end)];
        double average = bucketEntries.Length == 0
            ? 0
            : Math.Round(bucketEntries.Average(entry => entry.Weight), 2, MidpointRounding.ToEven);
        return new DashboardWeightSummaryReadModel(start, end, average);
    }

    private static DashboardWaistSummaryReadModel BuildWaistSummary(
        DateTime start,
        DateTime end,
        IReadOnlyList<WaistEntryReadModel> entries) {
        WaistEntryReadModel[] bucketEntries = [.. entries.Where(entry => entry.Date >= start && entry.Date <= end)];
        double average = bucketEntries.Length == 0
            ? 0
            : Math.Round(bucketEntries.Average(entry => entry.Circumference), 2, MidpointRounding.ToEven);
        return new DashboardWaistSummaryReadModel(start, end, average);
    }

    private static IEnumerable<(DateTime Start, DateTime End)> BuildBuckets(DateTime from, DateTime to, int step) {
        DateTime current = from.Date;
        DateTime end = to.Date;
        while (current <= end) {
            DateTime bucketEnd = current.AddDays(step - 1);
            if (bucketEnd > end) {
                bucketEnd = end;
            }

            yield return (current, bucketEnd);
            current = bucketEnd.AddDays(1);
        }
    }
}
