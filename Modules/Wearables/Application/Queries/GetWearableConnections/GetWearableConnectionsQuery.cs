using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;

namespace FoodDiary.Modules.Wearables.Application.Queries.GetWearableConnections;

public record GetWearableConnectionsQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WearableConnectionModel>>>, IUserRequest;
