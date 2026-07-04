using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Consumptions.Commands.DeleteConsumption;

public class DeleteConsumptionCommandHandler(
    IMealReadRepository mealReadRepository,
    IMealWriteRepository mealWriteRepository)
    : ICommandHandler<DeleteConsumptionCommand, Result> {
    public async Task<Result> Handle(DeleteConsumptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        if (command.ConsumptionId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.ConsumptionId), "Consumption id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var consumptionId = new MealId(command.ConsumptionId);

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
