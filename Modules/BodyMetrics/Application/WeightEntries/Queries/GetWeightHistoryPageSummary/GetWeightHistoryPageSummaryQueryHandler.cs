using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.BodyMetrics.Application.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.GetWeightHistoryPageSummary;

public sealed class GetWeightHistoryPageSummaryQueryHandler(
    ISender sender,
    IUserBodyMetricHistoryReadService userProfileReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWeightHistoryPageSummaryQuery, Result<WeightHistoryPageSummaryModel>> {
    public async Task<Result<WeightHistoryPageSummaryModel>> Handle(
        GetWeightHistoryPageSummaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WeightHistoryPageSummaryModel>(userIdResult);
        }

        Error? validationError = Validate(query);
        if (validationError is not null) {
            return Result.Failure<WeightHistoryPageSummaryModel>(validationError);
        }

        UserId userId = userIdResult.Value;
        DateTime dateFrom = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateFrom);
        DateTime dateTo = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateTo);
        Result<WeightHistoryProfileModel> profileResult = await userProfileReadService
            .GetWeightHistoryProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<WeightHistoryPageSummaryModel>(profileResult.Error);
        }

        IReadOnlyList<WeightEntryModel> entries = await sender.Send(new ReadWeightEntriesQuery(UserId: userId, DateFrom: null, DateTo: null, Limit: query.EntriesLimit, Descending: true), cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WeightEntrySummaryModel> summary = await sender.Send(new ReadWeightSummariesQuery(UserId: userId, DateFrom: dateFrom, DateTo: dateTo, QuantizationDays: query.QuantizationDays), cancellationToken).ConfigureAwait(false);
        WeightHistoryProfileModel profile = profileResult.Value;
        return Result.Success(new WeightHistoryPageSummaryModel(entries, summary, profile.HeightCm, profile.Goal, profile.GoalHistory));
    }

    private static Error? Validate(GetWeightHistoryPageSummaryQuery query) {
        if (query.DateFrom > query.DateTo) {
            return Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be earlier than DateTo.");
        }

        if (!TemporalRangePolicy.IsPeriodWithinLimit(query.DateFrom, query.DateTo)) {
            return Errors.Validation.Invalid(
                nameof(query.DateTo),
                $"The period must not exceed {TemporalRangePolicy.MaxPeriodDays} days.");
        }

        if (!TemporalRangePolicy.IsQuantizationValid(query.QuantizationDays)) {
            return Errors.Validation.Invalid(
                nameof(query.QuantizationDays),
                $"Value must be between 1 and {TemporalRangePolicy.MaxQuantizationDays}.");
        }

        return query.EntriesLimit is <= 0 or > 500
            ? Errors.Validation.Invalid(nameof(query.EntriesLimit), "Value must be between 1 and 500.")
            : null;
    }
}
