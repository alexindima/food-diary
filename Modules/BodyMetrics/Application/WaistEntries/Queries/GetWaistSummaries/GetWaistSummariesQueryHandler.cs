using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;
using FoodDiary.Application.BodyMetrics.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.BodyMetrics.WaistEntries.Queries.GetWaistSummaries;

public sealed class GetWaistSummariesQueryHandler(
    IWaistEntryReadService waistEntryReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWaistSummariesQuery, Result<IReadOnlyList<WaistEntrySummaryModel>>> {
    public async Task<Result<IReadOnlyList<WaistEntrySummaryModel>>> Handle(
        GetWaistSummariesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<WaistEntrySummaryModel>>(userIdResult);
        }

        if (query.DateFrom > query.DateTo) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be earlier than DateTo."));
        }

        if (!TemporalRangePolicy.IsPeriodWithinLimit(query.DateFrom, query.DateTo)) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(
                Errors.Validation.Invalid(
                    nameof(query.DateTo),
                    $"The period must not exceed {TemporalRangePolicy.MaxPeriodDays} days."));
        }

        if (!TemporalRangePolicy.IsQuantizationValid(query.QuantizationDays)) {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryModel>>(
                Errors.Validation.Invalid(
                    nameof(query.QuantizationDays),
                    $"Value must be between 1 and {TemporalRangePolicy.MaxQuantizationDays}."));
        }

        UserId userId = userIdResult.Value;
        DateTime normalizedFrom = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateTo);

        IReadOnlyList<WaistEntrySummaryModel> response = await waistEntryReadService.GetSummariesAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            query.QuantizationDays,
            cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WaistEntrySummaryModel>>(response);
    }
}
