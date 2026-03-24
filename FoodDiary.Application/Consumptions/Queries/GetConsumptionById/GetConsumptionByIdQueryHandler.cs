using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionById;

public class GetConsumptionByIdQueryHandler(IMealRepository mealRepository)
    : IQueryHandler<GetConsumptionByIdQuery, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(GetConsumptionByIdQuery request, CancellationToken cancellationToken) {
        if (request.UserId is null || request.UserId == Guid.Empty) {
            return Result.Failure<ConsumptionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(request.UserId.Value);

        var meal = await mealRepository.GetByIdAsync(
            request.ConsumptionId,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken);

        return meal is null
            ? Result.Failure<ConsumptionModel>(Errors.Consumption.NotFound(request.ConsumptionId.Value))
            : Result.Success(meal.ToModel());
    }
}
