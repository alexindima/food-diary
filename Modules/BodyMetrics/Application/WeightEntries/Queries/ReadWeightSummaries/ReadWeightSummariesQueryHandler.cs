using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadWeightSummaries;

public sealed class ReadWeightSummariesQueryHandler(IWeightEntryReadModelRepository weightEntryRepository) : IRequestHandler<ReadWeightSummariesQuery, IReadOnlyList<WeightEntrySummaryModel>> {
    public async Task<IReadOnlyList<WeightEntrySummaryModel>> Handle(ReadWeightSummariesQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime dateFrom = request.DateFrom;
        DateTime dateTo = request.DateTo;
        int quantizationDays = request.QuantizationDays;
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
