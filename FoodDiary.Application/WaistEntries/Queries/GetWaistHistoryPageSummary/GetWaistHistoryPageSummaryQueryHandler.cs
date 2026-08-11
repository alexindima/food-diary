using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistHistoryPageSummary;

public sealed class GetWaistHistoryPageSummaryQueryHandler(
    IWaistEntryReadService readService,
    IUserProfileReadService userProfileReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWaistHistoryPageSummaryQuery, Result<WaistHistoryPageSummaryModel>> {
    public async Task<Result<WaistHistoryPageSummaryModel>> Handle(
        GetWaistHistoryPageSummaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WaistHistoryPageSummaryModel>(userIdResult);
        }

        Error? validationError = Validate(query);
        if (validationError is not null) {
            return Result.Failure<WaistHistoryPageSummaryModel>(validationError);
        }

        UserId userId = userIdResult.Value;
        DateTime dateFrom = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateFrom);
        DateTime dateTo = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateTo);
        Result<WaistHistoryProfileModel> profileResult = await userProfileReadService
            .GetWaistHistoryProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<WaistHistoryPageSummaryModel>(profileResult.Error);
        }

        IReadOnlyList<WaistEntryModel> entries = await readService.GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: query.EntriesLimit,
            descending: true,
            cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WaistEntrySummaryModel> summary = await readService.GetSummariesAsync(
            userId, dateFrom, dateTo, query.QuantizationDays, cancellationToken).ConfigureAwait(false);
        WaistHistoryProfileModel profile = profileResult.Value;
        return Result.Success(new WaistHistoryPageSummaryModel(entries, summary, profile.Height, profile.Goal, profile.GoalHistory));
    }

    private static Error? Validate(GetWaistHistoryPageSummaryQuery query) {
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
