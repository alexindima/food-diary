using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.DeleteConsumption;

public class DeleteConsumptionCommandHandler(IMealRepository mealRepository)
    : ICommandHandler<DeleteConsumptionCommand, Result<bool>> {
    public async Task<Result<bool>> Handle(DeleteConsumptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);

        var meal = await mealRepository.GetByIdAsync(
            command.ConsumptionId,
            userId,
            includeItems: false,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (meal is null) {
            return Result.Failure<bool>(Errors.Consumption.NotFound(command.ConsumptionId.Value));
        }

        await mealRepository.DeleteAsync(meal, cancellationToken);
        return Result.Success(true);
    }
}
