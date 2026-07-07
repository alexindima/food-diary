using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Consumptions.Commands.DeleteConsumption;

public sealed class DeleteConsumptionCommandHandler(
    IMealReadRepository mealReadRepository,
    IMealWriteRepository mealWriteRepository)
    : ICommandHandler<DeleteConsumptionCommand, Result> {
    public async Task<Result> Handle(DeleteConsumptionCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        Result<MealId> consumptionIdResult = RequiredIdParser.Parse(
            command.ConsumptionId,
            nameof(command.ConsumptionId),
            "Consumption id must not be empty.",
            value => new MealId(value));
        if (consumptionIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(consumptionIdResult);
        }

        UserId userId = userIdResult.Value;
        MealId consumptionId = consumptionIdResult.Value;

        Meal? meal = await mealReadRepository.GetByIdAsync(
            consumptionId,
            userId,
            includeItems: false,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (meal is null) {
            return Result.Failure(Errors.Consumption.NotFound(command.ConsumptionId));
        }

        await mealWriteRepository.DeleteAsync(meal, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
