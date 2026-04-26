using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Wearables.Models;

namespace FoodDiary.Application.Wearables.Queries.GetWearableConnections;

public record GetWearableConnectionsQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WearableConnectionModel>>>, IUserRequest;
