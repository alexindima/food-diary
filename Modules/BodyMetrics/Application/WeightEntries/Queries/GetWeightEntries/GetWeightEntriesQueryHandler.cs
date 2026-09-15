using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Application.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.GetWeightEntries;

public sealed class GetWeightEntriesQueryHandler(
    ISender sender,
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

        IReadOnlyList<WeightEntryModel> response = await sender.Send(new ReadWeightEntriesQuery(UserId: userId, DateFrom: normalizedFrom, DateTo: normalizedTo, Limit: PaginationPolicy.NormalizeCollectionLimit(query.Limit), Descending: query.Descending), cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<WeightEntryModel>>(response);
    }
}
