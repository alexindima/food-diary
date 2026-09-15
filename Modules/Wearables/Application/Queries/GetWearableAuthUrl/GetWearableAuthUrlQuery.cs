using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Wearables.Application.Queries.GetWearableAuthUrl;

public record GetWearableAuthUrlQuery(Guid? UserId, string Provider, string State)
    : IQuery<Result<string>>, IUserRequest;
