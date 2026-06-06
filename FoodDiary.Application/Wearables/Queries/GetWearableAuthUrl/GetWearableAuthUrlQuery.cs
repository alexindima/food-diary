using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;

public record GetWearableAuthUrlQuery(Guid? UserId, string Provider, string State)
    : IQuery<Result<string>>, IUserRequest;
