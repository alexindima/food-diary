using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public class CreateCycleCommandHandler(ICycleRepository cycleRepository)
    : ICommandHandler<CreateCycleCommand, Result<CycleModel>> {
    public async Task<Result<CycleModel>> Handle(
        CreateCycleCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CycleModel>(Errors.User.NotFound());
        }

        var userId = new UserId(command.UserId.Value);

        var cycle = Cycle.Create(
            userId,
            command.StartDate,
            command.AverageLength,
            command.LutealLength,
            command.Notes);

        cycle = await cycleRepository.AddAsync(cycle, cancellationToken);

        var predictions = CyclePredictionService.CalculatePredictions(cycle);
        return Result.Success(cycle.ToModel(predictions));
    }
}
