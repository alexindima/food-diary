using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionById;

public record GetConsumptionByIdQuery(UserId? UserId, MealId ConsumptionId)
    : IQuery<Result<ConsumptionModel>>;
