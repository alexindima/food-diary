using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Wearables;

namespace FoodDiary.Application.Wearables.Queries.GetWearableConnections;

public class GetWearableConnectionsQueryHandler(IWearableConnectionRepository repository)
    : IQueryHandler<GetWearableConnectionsQuery, Result<IReadOnlyList<WearableConnectionModel>>> {
    public async Task<Result<IReadOnlyList<WearableConnectionModel>>> Handle(
        GetWearableConnectionsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WearableConnectionModel>>(userIdResult.Error);
        }

        IReadOnlyList<WearableConnection> connections = await repository.GetAllForUserAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);

        var models = connections
            .Select(c => new WearableConnectionModel(
                c.Provider.ToString(),
                c.ExternalUserId,
                c.IsActive,
                c.LastSyncedAtUtc,
                c.CreatedOnUtc))
            .ToList();

        return Result.Success<IReadOnlyList<WearableConnectionModel>>(models);
    }
}
