using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionById;

public sealed class GetConsumptionByIdQueryHandler(IConsumptionReadService consumptionReadService)
    : IQueryHandler<GetConsumptionByIdQuery, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(GetConsumptionByIdQuery request, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(request.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<ConsumptionModel>(userIdResult);
        }

        Result<MealId> consumptionIdResult = RequiredIdParser.Parse(
            request.ConsumptionId,
            nameof(request.ConsumptionId),
            "Consumption id must not be empty.",
            value => new MealId(value));
        if (consumptionIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<ConsumptionModel, MealId>(consumptionIdResult);
        }

        UserId userId = userIdResult.Value;
        MealId consumptionId = consumptionIdResult.Value;

        ConsumptionModel? consumption = await consumptionReadService.GetByIdAsync(
            userId,
            consumptionId,
            cancellationToken).ConfigureAwait(false);

        return consumption is null
            ? Result.Failure<ConsumptionModel>(Errors.Consumption.NotFound(request.ConsumptionId))
            : Result.Success(consumption);
    }
}
