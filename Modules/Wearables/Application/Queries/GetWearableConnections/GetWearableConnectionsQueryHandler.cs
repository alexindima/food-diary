using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Application.Queries.GetWearableConnections;

internal sealed class GetWearableConnectionsQueryHandler(
    IWearableConnectionReadRepository connectionRepository,
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

        IReadOnlyList<WearableConnectionModel> models = await connectionRepository
            .GetConnectionModelsAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<WearableConnectionModel>>(models);
    }
}
