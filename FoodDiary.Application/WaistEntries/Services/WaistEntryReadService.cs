using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Services;

internal sealed class WaistEntryReadService(IWaistEntryReadRepository waistEntryRepository) : IWaistEntryReadService {
    public async Task<IReadOnlyList<WaistEntryModel>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken) {
        IReadOnlyList<WaistEntry> entries = await waistEntryRepository.GetEntriesAsync(
            userId,
            dateFrom,
            dateTo,
            limit,
            descending,
            cancellationToken).ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];
    }

    public async Task<WaistEntryModel?> GetLatestAsync(UserId userId, CancellationToken cancellationToken) {
        IReadOnlyList<WaistEntryModel> entries = await GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: 1,
            descending: true,
            cancellationToken).ConfigureAwait(false);

        return entries.Count > 0 ? entries[0] : null;
    }

    public async Task<IReadOnlyList<WaistEntrySummaryModel>> GetSummariesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        CancellationToken cancellationToken) {
        IReadOnlyList<WaistEntry> entries = await waistEntryRepository.GetByPeriodAsync(
            userId,
            dateFrom,
            dateTo,
            cancellationToken).ConfigureAwait(false);

        return [.. BuildBuckets(dateFrom, dateTo, quantizationDays)
            .Select(bucket => BuildResponse(bucket.start, bucket.end, entries))];
    }

    private static IEnumerable<(DateTime start, DateTime end)> BuildBuckets(DateTime from, DateTime to, int step) {
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

    private static WaistEntrySummaryModel BuildResponse(
        DateTime start,
        DateTime end,
        IReadOnlyList<WaistEntry> entries) {
        List<WaistEntry> bucketEntries = [.. entries.Where(entry => entry.Date >= start && entry.Date <= end)];

        if (bucketEntries.Count == 0) {
            return new WaistEntrySummaryModel(start, end, 0);
        }

        double avg = bucketEntries.Average(entry => entry.Circumference);
        return new WaistEntrySummaryModel(start, end, Math.Round(avg, 2, MidpointRounding.ToEven));
    }
}
