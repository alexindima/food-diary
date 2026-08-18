using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.BodyMetrics.WeightEntries.Mappings;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.BodyMetrics.WeightEntries.Services;

internal sealed class WeightEntryReadService(IWeightEntryReadModelRepository weightEntryRepository) : IWeightEntryReadService {
    public async Task<IReadOnlyList<WeightEntryModel>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken) {
        IReadOnlyList<WeightEntryReadModel> entries = await weightEntryRepository.GetEntryReadModelsAsync(
            userId,
            dateFrom,
            dateTo,
            limit,
            descending,
            cancellationToken).ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];
    }

    public async Task<WeightEntryModel?> GetLatestAsync(UserId userId, CancellationToken cancellationToken) {
        IReadOnlyList<WeightEntryModel> entries = await GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: 1,
            descending: true,
            cancellationToken).ConfigureAwait(false);

        return entries.Count > 0 ? entries[0] : null;
    }

    public async Task<IReadOnlyList<WeightEntrySummaryModel>> GetSummariesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        CancellationToken cancellationToken) {
        IReadOnlyList<WeightEntryReadModel> entries = await weightEntryRepository.GetByPeriodReadModelsAsync(
            userId,
            dateFrom,
            dateTo,
            cancellationToken).ConfigureAwait(false);

        return [.. TemporalRangePolicy.BuildDateBuckets(dateFrom, dateTo, quantizationDays)
            .Select(bucket => BuildResponse(bucket.Start, bucket.End, entries))];
    }

    private static WeightEntrySummaryModel BuildResponse(
        DateTime start,
        DateTime end,
        IReadOnlyList<WeightEntryReadModel> entries) {
        List<WeightEntryReadModel> bucketEntries = [.. entries.Where(entry => entry.Date >= start && entry.Date <= end)];

        if (bucketEntries.Count == 0) {
            return new WeightEntrySummaryModel(start, end, 0);
        }

        double avg = bucketEntries.Average(entry => entry.WeightKg);
        return new WeightEntrySummaryModel(start, end, Math.Round(avg, 2, MidpointRounding.ToEven));
    }
}
