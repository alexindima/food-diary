using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Application.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.GetWeightSummaries;

public sealed class GetWeightSummariesQueryHandler(
    ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWeightSummariesQuery, Result<IReadOnlyList<WeightEntrySummaryModel>>> {
    public async Task<Result<IReadOnlyList<WeightEntrySummaryModel>>> Handle(
        GetWeightSummariesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<WeightEntrySummaryModel>>(userIdResult);
        }

        if (query.DateFrom > query.DateTo) {
            return Result.Failure<IReadOnlyList<WeightEntrySummaryModel>>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be earlier than DateTo."));
        }

        if (!TemporalRangePolicy.IsPeriodWithinLimit(query.DateFrom, query.DateTo)) {
            return Result.Failure<IReadOnlyList<WeightEntrySummaryModel>>(
                Errors.Validation.Invalid(
                    nameof(query.DateTo),
                    $"The period must not exceed {TemporalRangePolicy.MaxPeriodDays} days."));
        }

        if (!TemporalRangePolicy.IsQuantizationValid(query.QuantizationDays)) {
            return Result.Failure<IReadOnlyList<WeightEntrySummaryModel>>(
                Errors.Validation.Invalid(
                    nameof(query.QuantizationDays),
                    $"Value must be between 1 and {TemporalRangePolicy.MaxQuantizationDays}."));
        }

        UserId userId = userIdResult.Value;
        DateTime normalizedFrom = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateTo);

        IReadOnlyList<WeightEntrySummaryModel> response = await sender.Send(new ReadWeightSummariesQuery(UserId: userId, DateFrom: normalizedFrom, DateTo: normalizedTo, QuantizationDays: query.QuantizationDays), cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WeightEntrySummaryModel>>(response);
    }
}
