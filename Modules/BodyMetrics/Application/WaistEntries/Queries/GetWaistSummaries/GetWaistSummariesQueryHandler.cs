using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Application.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetWaistSummaries;

public sealed class GetWaistSummariesQueryHandler(
    ISender sender,
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

        IReadOnlyList<WaistEntrySummaryModel> response = await sender.Send(new ReadWaistSummariesQuery(UserId: userId, DateFrom: normalizedFrom, DateTo: normalizedTo, QuantizationDays: query.QuantizationDays), cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WaistEntrySummaryModel>>(response);
    }
}
