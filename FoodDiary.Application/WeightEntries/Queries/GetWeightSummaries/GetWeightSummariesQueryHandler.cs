using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;

public sealed class GetWeightSummariesQueryHandler(
    IWeightEntryReadService weightEntryReadService,
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

        IReadOnlyList<WeightEntrySummaryModel> response = await weightEntryReadService.GetSummariesAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            query.QuantizationDays,
            cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WeightEntrySummaryModel>>(response);
    }
}
