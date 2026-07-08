using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Queries.GetWearableConnections;

public sealed class GetWearableConnectionsQueryHandler(
    IWearableReadService wearableReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWearableConnectionsQuery, Result<IReadOnlyList<WearableConnectionModel>>> {
    public async Task<Result<IReadOnlyList<WearableConnectionModel>>> Handle(
        GetWearableConnectionsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<WearableConnectionModel>>(userIdResult);
        }

        IReadOnlyList<WearableConnectionModel> models = await wearableReadService
            .GetConnectionsAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<WearableConnectionModel>>(models);
    }
}
