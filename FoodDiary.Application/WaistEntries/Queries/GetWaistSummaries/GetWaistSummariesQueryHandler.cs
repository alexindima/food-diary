using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;

public class GetWaistSummariesQueryHandler(IWaistEntryRepository waistEntryRepository)
    : IQueryHandler<GetWaistSummariesQuery, Result<IReadOnlyList<WaistEntrySummaryModel>>> {
    public async Task<Result<IReadOnlyList<WaistEntrySummaryModel>>> Handle(
        GetWaistSummariesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == Guid.Empty) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(Errors.Authentication.InvalidToken);
        }

        if (query.DateFrom > query.DateTo) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be earlier than DateTo."));
        }

        if (query.QuantizationDays <= 0) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(
                Errors.Validation.Invalid(nameof(query.QuantizationDays), "Value must be greater than zero."));
        }

        var userId = new UserId(query.UserId.Value);
        var normalizedFrom = NormalizeUtcDate(query.DateFrom);
        var normalizedTo = NormalizeUtcDate(query.DateTo);

        var entries = await waistEntryRepository.GetByPeriodAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            cancellationToken);

        var buckets = BuildBuckets(normalizedFrom, normalizedTo, query.QuantizationDays);
        var response = buckets
            .Select(bucket => BuildResponse(bucket.start, bucket.end, entries))
            .ToList();

        return Result.Success<IReadOnlyList<WaistEntrySummaryModel>>(response);
    }

    private static IEnumerable<(DateTime start, DateTime end)> BuildBuckets(DateTime from, DateTime to, int step) {
        var current = from.Date;
        var end = to.Date;
        while (current <= end) {
            var bucketEnd = current.AddDays(step - 1);
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
        var bucketEntries = entries
            .Where(entry => entry.Date >= start && entry.Date <= end)
            .ToList();

        if (bucketEntries.Count == 0) {
            return new WaistEntrySummaryModel(start, end, 0);
        }

        var avg = bucketEntries.Average(entry => entry.Circumference);
        return new WaistEntrySummaryModel(start, end, Math.Round(avg, 2));
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
