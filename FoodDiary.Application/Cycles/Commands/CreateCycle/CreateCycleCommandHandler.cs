using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public class CreateCycleCommandHandler(
    ICycleRepository cycleRepository,
    IUserRepository userRepository)
    : ICommandHandler<CreateCycleCommand, Result<CycleModel>> {
    public async Task<Result<CycleModel>> Handle(
        CreateCycleCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CycleModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CycleModel>(accessError);
        }

        var cycle = Cycle.Create(
            userId,
            command.StartDate,
            command.AverageLength,
            command.LutealLength,
            command.Notes);

        cycle = await cycleRepository.AddAsync(cycle, cancellationToken).ConfigureAwait(false);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(cycle);
        return Result.Success(cycle.ToModel(predictions));
    }
}
