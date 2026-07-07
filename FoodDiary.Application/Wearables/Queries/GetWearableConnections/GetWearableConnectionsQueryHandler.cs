using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Queries.GetWearableConnections;

public sealed class GetWearableConnectionsQueryHandler(IWearableReadService wearableReadService)
    : IQueryHandler<GetWearableConnectionsQuery, Result<IReadOnlyList<WearableConnectionModel>>> {
    public async Task<Result<IReadOnlyList<WearableConnectionModel>>> Handle(
        GetWearableConnectionsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<WearableConnectionModel>>(userIdResult);
        }

        IReadOnlyList<WearableConnectionModel> models = await wearableReadService
            .GetConnectionsAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<WearableConnectionModel>>(models);
    }
}
