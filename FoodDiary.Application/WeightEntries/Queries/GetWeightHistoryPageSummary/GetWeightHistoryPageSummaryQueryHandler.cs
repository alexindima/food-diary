using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightHistoryPageSummary;

public sealed class GetWeightHistoryPageSummaryQueryHandler(
    IWeightEntryReadService readService,
    IUserProfileReadService userProfileReadService,
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

        IReadOnlyList<WeightEntryModel> entries = await readService.GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: query.EntriesLimit,
            descending: true,
            cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WeightEntrySummaryModel> summary = await readService.GetSummariesAsync(
            userId, dateFrom, dateTo, query.QuantizationDays, cancellationToken).ConfigureAwait(false);
        WeightHistoryProfileModel profile = profileResult.Value;
        return Result.Success(new WeightHistoryPageSummaryModel(entries, summary, profile.Height, profile.Goal, profile.GoalHistory));
    }

    private static Error? Validate(GetWeightHistoryPageSummaryQuery query) {
        if (query.DateFrom > query.DateTo) {
            return Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be earlier than DateTo.");
        }

        if (query.QuantizationDays <= 0) {
            return Errors.Validation.Invalid(nameof(query.QuantizationDays), "Value must be greater than zero.");
        }

        return query.EntriesLimit is <= 0 or > 500
            ? Errors.Validation.Invalid(nameof(query.EntriesLimit), "Value must be between 1 and 500.")
            : null;
    }
}
