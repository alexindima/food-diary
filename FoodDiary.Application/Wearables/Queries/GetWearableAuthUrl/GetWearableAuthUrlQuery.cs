using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;

public record GetWearableAuthUrlQuery(string Provider, string State)
    : IQuery<Result<string>>;
