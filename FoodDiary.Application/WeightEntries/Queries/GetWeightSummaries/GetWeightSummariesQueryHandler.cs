using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;

public class GetWeightSummariesQueryHandler(
    IWeightEntryRepository weightEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWeightSummariesQuery, Result<IReadOnlyList<WeightEntrySummaryModel>>> {
    public async Task<Result<IReadOnlyList<WeightEntrySummaryModel>>> Handle(
        GetWeightSummariesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WeightEntrySummaryModel>>(userIdResult.Error);
        }

        if (query.DateFrom > query.DateTo) {
            return Result.Failure<IReadOnlyList<WeightEntrySummaryModel>>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be earlier than DateTo."));
        }

        if (query.QuantizationDays <= 0) {
            return Result.Failure<IReadOnlyList<WeightEntrySummaryModel>>(
                Errors.Validation.Invalid(nameof(query.QuantizationDays), "Value must be greater than zero."));
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<WeightEntrySummaryModel>>(accessError);
        }

        DateTime normalizedFrom = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateTo);

        IReadOnlyList<WeightEntry> entries = await weightEntryRepository.GetByPeriodAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            cancellationToken).ConfigureAwait(false);

        IEnumerable<(DateTime start, DateTime end)> buckets = BuildBuckets(normalizedFrom, normalizedTo, query.QuantizationDays);
        var response = buckets
            .Select(bucket => BuildResponse(bucket.start, bucket.end, entries))
            .ToList();

        return Result.Success<IReadOnlyList<WeightEntrySummaryModel>>(response);
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

    private static WeightEntrySummaryModel BuildResponse(
        DateTime start,
        DateTime end,
        IReadOnlyList<WeightEntry> entries) {
        var bucketEntries = entries
            .Where(entry => entry.Date >= start && entry.Date <= end)
            .ToList();

        if (bucketEntries.Count == 0) {
            return new WeightEntrySummaryModel(start, end, 0);
        }

        double avg = bucketEntries.Average(entry => entry.Weight);
        return new WeightEntrySummaryModel(start, end, Math.Round(avg, 2, MidpointRounding.ToEven));
    }
}
