using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public sealed class GetHydrationEntriesQueryHandler(
    IHydrationEntryReadService hydrationEntryReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetHydrationEntriesQuery, Result<IReadOnlyList<HydrationEntryModel>>> {
    public async Task<Result<IReadOnlyList<HydrationEntryModel>>> Handle(
        GetHydrationEntriesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<HydrationEntryModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DateTime dateUtc = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateUtc);
        IReadOnlyList<HydrationEntryModel> response = await hydrationEntryReadService
            .GetEntriesByDateAsync(userId, dateUtc, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<HydrationEntryModel>>(response);
    }
}
