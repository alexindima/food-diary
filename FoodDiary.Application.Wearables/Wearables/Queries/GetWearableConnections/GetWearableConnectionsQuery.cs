using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Wearables.Models;

namespace FoodDiary.Application.Wearables.Wearables.Queries.GetWearableConnections;

public record GetWearableConnectionsQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WearableConnectionModel>>>, IUserRequest;
