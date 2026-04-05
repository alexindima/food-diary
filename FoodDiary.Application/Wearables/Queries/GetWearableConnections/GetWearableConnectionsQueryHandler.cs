using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Application.Wearables.Models;

namespace FoodDiary.Application.Wearables.Queries.GetWearableConnections;

public class GetWearableConnectionsQueryHandler(IWearableConnectionRepository repository)
    : IQueryHandler<GetWearableConnectionsQuery, Result<IReadOnlyList<WearableConnectionModel>>> {
    public async Task<Result<IReadOnlyList<WearableConnectionModel>>> Handle(
        GetWearableConnectionsQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WearableConnectionModel>>(userIdResult.Error);
        }

        var connections = await repository.GetAllForUserAsync(userIdResult.Value, cancellationToken);

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
