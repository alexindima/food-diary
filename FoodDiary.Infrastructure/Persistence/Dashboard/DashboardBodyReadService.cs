using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Dashboard;

internal sealed class DashboardBodyReadService(FoodDiaryDbContext context) : IDashboardBodyReadService {
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
        int normalizedTrendQuantizationDays = Math.Clamp(
            trendQuantizationDays <= 0 ? 1 : trendQuantizationDays,
            1,
            TemporalRangePolicy.MaxQuantizationDays);
        DateTime normalizedDayStart = NormalizeUtcDate(dayStart);
        DateTime normalizedDayEndStart = NormalizeUtcDate(dayEndStart);
        DateTime normalizedTrendStart = NormalizeUtcDate(trendStart);

        IReadOnlyList<DashboardWeightPointReadModel> latestWeightEntries = includeWeight
            ? await GetLatestWeightEntriesAsync(userId, normalizedDayEndStart, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<DashboardWaistPointReadModel> latestWaistEntries = includeWaist
            ? await GetLatestWaistEntriesAsync(userId, normalizedDayEndStart, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<DashboardWeightPointReadModel> weightTrendEntries = includeWeight
            ? await GetWeightTrendEntriesAsync(userId, normalizedTrendStart, normalizedDayStart, cancellationToken).ConfigureAwait(false)
            : [];
        IReadOnlyList<DashboardWaistPointReadModel> waistTrendEntries = includeWaist
            ? await GetWaistTrendEntriesAsync(userId, normalizedTrendStart, normalizedDayStart, cancellationToken).ConfigureAwait(false)
            : [];
        int hydrationTotalMl = includeHydration
            ? await GetHydrationTotalAsync(userId, dayStart, dayEndStart, cancellationToken).ConfigureAwait(false)
            : 0;

        return new DashboardBodyReadModel(
            latestWeightEntries,
            latestWaistEntries,
            BuildWeightTrend(normalizedTrendStart, normalizedDayStart, normalizedTrendQuantizationDays, weightTrendEntries),
            BuildWaistTrend(normalizedTrendStart, normalizedDayStart, normalizedTrendQuantizationDays, waistTrendEntries),
            hydrationTotalMl);
    }

    private async Task<IReadOnlyList<DashboardWeightPointReadModel>> GetLatestWeightEntriesAsync(
        UserId userId,
        DateTime dayEndStart,
        CancellationToken cancellationToken) {
        return await context.WeightEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Date <= dayEndStart)
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.CreatedOnUtc)
            .Take(2)
            .Select(entry => new DashboardWeightPointReadModel(entry.Date, entry.WeightKg))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<DashboardWaistPointReadModel>> GetLatestWaistEntriesAsync(
        UserId userId,
        DateTime dayEndStart,
        CancellationToken cancellationToken) {
        return await context.WaistEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Date <= dayEndStart)
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.CreatedOnUtc)
            .Take(2)
            .Select(entry => new DashboardWaistPointReadModel(entry.Date, entry.CircumferenceCm))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<DashboardWeightPointReadModel>> GetWeightTrendEntriesAsync(
        UserId userId,
        DateTime trendStart,
        DateTime dayStart,
        CancellationToken cancellationToken) {
        return await context.WeightEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Date >= trendStart && entry.Date <= dayStart)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.CreatedOnUtc)
            .Select(entry => new DashboardWeightPointReadModel(entry.Date, entry.WeightKg))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<DashboardWaistPointReadModel>> GetWaistTrendEntriesAsync(
        UserId userId,
        DateTime trendStart,
        DateTime dayStart,
        CancellationToken cancellationToken) {
        return await context.WaistEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Date >= trendStart && entry.Date <= dayStart)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.CreatedOnUtc)
            .Select(entry => new DashboardWaistPointReadModel(entry.Date, entry.CircumferenceCm))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> GetHydrationTotalAsync(
        UserId userId,
        DateTime dayStart,
        DateTime dayEndStart,
        CancellationToken cancellationToken) {
        return await context.HydrationEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Timestamp >= dayStart && entry.Timestamp <= dayEndStart)
            .SumAsync(entry => entry.AmountMl, cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<DashboardWeightSummaryReadModel> BuildWeightTrend(
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        IReadOnlyList<DashboardWeightPointReadModel> entries) =>
        [.. TemporalRangePolicy.BuildDateBuckets(dateFrom, dateTo, quantizationDays)
            .Select(bucket => BuildWeightSummary(bucket.Start, bucket.End, entries))];

    private static IReadOnlyList<DashboardWaistSummaryReadModel> BuildWaistTrend(
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        IReadOnlyList<DashboardWaistPointReadModel> entries) =>
        [.. TemporalRangePolicy.BuildDateBuckets(dateFrom, dateTo, quantizationDays)
            .Select(bucket => BuildWaistSummary(bucket.Start, bucket.End, entries))];

    private static DashboardWeightSummaryReadModel BuildWeightSummary(
        DateTime start,
        DateTime end,
        IReadOnlyList<DashboardWeightPointReadModel> entries) {
        DashboardWeightPointReadModel[] bucketEntries = [.. entries.Where(entry => entry.Date >= start && entry.Date <= end)];
        double average = bucketEntries.Length == 0
            ? 0
            : Math.Round(bucketEntries.Average(entry => entry.WeightKg), 2, MidpointRounding.ToEven);
        return new DashboardWeightSummaryReadModel(start, end, average);
    }

    private static DashboardWaistSummaryReadModel BuildWaistSummary(
        DateTime start,
        DateTime end,
        IReadOnlyList<DashboardWaistPointReadModel> entries) {
        DashboardWaistPointReadModel[] bucketEntries = [.. entries.Where(entry => entry.Date >= start && entry.Date <= end)];
        double average = bucketEntries.Length == 0
            ? 0
            : Math.Round(bucketEntries.Average(entry => entry.CircumferenceCm), 2, MidpointRounding.ToEven);
        return new DashboardWaistSummaryReadModel(start, end, average);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        DateTime date = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime().Date
            : value.Date;
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}
