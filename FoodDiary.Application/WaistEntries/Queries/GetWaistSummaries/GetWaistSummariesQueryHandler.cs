using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;

public class GetWaistSummariesQueryHandler(
    IWaistEntryRepository waistEntryRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetWaistSummariesQuery, Result<IReadOnlyList<WaistEntrySummaryModel>>> {
    public async Task<Result<IReadOnlyList<WaistEntrySummaryModel>>> Handle(
        GetWaistSummariesQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(userIdResult.Error);
        }

        if (query.DateFrom > query.DateTo) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be earlier than DateTo."));
        }

        if (query.QuantizationDays <= 0) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(
                Errors.Validation.Invalid(nameof(query.QuantizationDays), "Value must be greater than zero."));
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(accessError);
        }

        var normalizedFrom = UtcDateNormalizer.NormalizeDateUsingLocalFallback(query.DateFrom);
        var normalizedTo = UtcDateNormalizer.NormalizeDateUsingLocalFallback(query.DateTo);

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
}
