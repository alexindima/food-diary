using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;

public class GetWaistSummariesQueryHandler(IWaistEntryRepository waistEntryRepository)
    : IQueryHandler<GetWaistSummariesQuery, Result<IReadOnlyList<WaistEntrySummaryResponse>>> {
    public async Task<Result<IReadOnlyList<WaistEntrySummaryResponse>>> Handle(
        GetWaistSummariesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == UserId.Empty) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryResponse>>(Errors.Authentication.InvalidToken);
        }

        if (query.DateFrom > query.DateTo) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryResponse>>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be earlier than DateTo."));
        }

        if (query.QuantizationDays <= 0) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryResponse>>(
                Errors.Validation.Invalid(nameof(query.QuantizationDays), "Value must be greater than zero."));
        }

        var normalizedFrom = NormalizeUtcDate(query.DateFrom);
        var normalizedTo = NormalizeUtcDate(query.DateTo);

        var entries = await waistEntryRepository.GetByPeriodAsync(
            query.UserId.Value,
            normalizedFrom,
            normalizedTo,
            cancellationToken);

        var buckets = BuildBuckets(normalizedFrom, normalizedTo, query.QuantizationDays);
        var response = buckets
            .Select(bucket => BuildResponse(bucket.start, bucket.end, entries))
            .ToList();

        return Result.Success<IReadOnlyList<WaistEntrySummaryResponse>>(response);
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

    private static WaistEntrySummaryResponse BuildResponse(
        DateTime start,
        DateTime end,
        IReadOnlyList<WaistEntry> entries) {
        var bucketEntries = entries
            .Where(entry => entry.Date >= start && entry.Date <= end)
            .ToList();

        if (bucketEntries.Count == 0) {
            return new WaistEntrySummaryResponse(start, end, 0);
        }

        var avg = bucketEntries.Average(entry => entry.Circumference);
        return new WaistEntrySummaryResponse(start, end, Math.Round(avg, 2));
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
