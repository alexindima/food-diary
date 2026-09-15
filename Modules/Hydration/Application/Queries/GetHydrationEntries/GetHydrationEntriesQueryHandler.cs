using FoodDiary.Mediator;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationEntries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Hydration.Application.Internal;
using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Application.Queries.GetHydrationEntries;

public sealed class GetHydrationEntriesQueryHandler(
    ISender sender,
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
        IReadOnlyList<HydrationEntryModel> response = await sender.Send(new ReadHydrationEntriesQuery(userId, dateUtc), cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<HydrationEntryModel>>(response);
    }
}
