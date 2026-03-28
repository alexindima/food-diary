using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.DeleteConsumption;

public class DeleteConsumptionCommandHandler(IMealRepository mealRepository)
    : ICommandHandler<DeleteConsumptionCommand, Result> {
    public async Task<Result> Handle(DeleteConsumptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var consumptionId = new MealId(command.ConsumptionId);

        var meal = await mealRepository.GetByIdAsync(
            consumptionId,
            userId,
            includeItems: false,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (meal is null) {
            return Result.Failure(Errors.Consumption.NotFound(command.ConsumptionId));
        }

        await mealRepository.DeleteAsync(meal, cancellationToken);
        return Result.Success();
    }
}
