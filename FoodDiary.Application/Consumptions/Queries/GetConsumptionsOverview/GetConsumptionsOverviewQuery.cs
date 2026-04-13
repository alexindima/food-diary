using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;

public sealed record GetConsumptionsOverviewQuery(
    Guid? UserId,
    int Page,
    int Limit,
    DateTime? DateFrom,
    DateTime? DateTo,
    int FavoriteLimit = 10)
    : IQuery<Result<ConsumptionOverviewModel>>, IUserRequest;
