using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.BodyMetrics.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.BodyMetrics.WeightEntries.Queries.GetWeightEntries;

public sealed class GetWeightEntriesQueryHandler(
    IWeightEntryReadService weightEntryReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWeightEntriesQuery, Result<IReadOnlyList<WeightEntryModel>>> {
    public async Task<Result<IReadOnlyList<WeightEntryModel>>> Handle(
        GetWeightEntriesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<WeightEntryModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DateTime? normalizedFrom = query.DateFrom.HasValue
            ? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateFrom.Value)
            : null;
        DateTime? normalizedTo = query.DateTo.HasValue
            ? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateTo.Value)
            : null;

        IReadOnlyList<WeightEntryModel> response = await weightEntryReadService.GetEntriesAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            query.Limit,
            query.Descending,
            cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WeightEntryModel>>(response);
    }
}
