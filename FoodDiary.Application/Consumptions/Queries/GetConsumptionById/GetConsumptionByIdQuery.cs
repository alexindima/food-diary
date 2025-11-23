using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionById;

public record GetConsumptionByIdQuery(UserId? UserId, MealId ConsumptionId)
    : IQuery<Result<ConsumptionResponse>>;
