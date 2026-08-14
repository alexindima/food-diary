using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Wearables.Wearables.Queries.GetWearableAuthUrl;

public record GetWearableAuthUrlQuery(Guid? UserId, string Provider, string State)
    : IQuery<Result<string>>, IUserRequest;
