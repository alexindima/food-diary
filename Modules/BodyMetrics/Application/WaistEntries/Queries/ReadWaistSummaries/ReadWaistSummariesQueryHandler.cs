using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.ReadWaistSummaries;

public sealed class ReadWaistSummariesQueryHandler(IWaistEntryReadModelRepository waistEntryRepository) : IRequestHandler<ReadWaistSummariesQuery, IReadOnlyList<WaistEntrySummaryModel>> {
    public async Task<IReadOnlyList<WaistEntrySummaryModel>> Handle(ReadWaistSummariesQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime dateFrom = request.DateFrom;
        DateTime dateTo = request.DateTo;
        int quantizationDays = request.QuantizationDays;
        IReadOnlyList<WaistEntryReadModel> entries = await waistEntryRepository.GetByPeriodReadModelsAsync(
            userId,
            dateFrom,
            dateTo,
            cancellationToken).ConfigureAwait(false);

        return [.. TemporalRangePolicy.BuildDateBuckets(dateFrom, dateTo, quantizationDays)
            .Select(bucket => BuildResponse(bucket.Start, bucket.End, entries))];

    }

    private static WaistEntrySummaryModel BuildResponse(
        DateTime start,
        DateTime end,
        IReadOnlyList<WaistEntryReadModel> entries) {
        List<WaistEntryReadModel> bucketEntries = [.. entries.Where(entry => entry.Date >= start && entry.Date <= end)];

        if (bucketEntries.Count == 0) {
            return new WaistEntrySummaryModel(start, end, 0);
        }

        double avg = bucketEntries.Average(entry => entry.CircumferenceCm);
        return new WaistEntrySummaryModel(start, end, Math.Round(avg, 2, MidpointRounding.ToEven));
    }

}
