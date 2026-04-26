using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionById;

public record GetConsumptionByIdQuery(Guid? UserId, Guid ConsumptionId)
    : IQuery<Result<ConsumptionModel>>, IUserRequest;
