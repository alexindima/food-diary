using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public class UpsertCycleDayCommandHandler(
    ICycleRepository cycleRepository,
    IUserRepository userRepository)
    : ICommandHandler<UpsertCycleDayCommand, Result<CycleDayModel>> {
    public async Task<Result<CycleDayModel>> Handle(
        UpsertCycleDayCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CycleDayModel>(Errors.Authentication.InvalidToken);
        }

        if (command.CycleId == Guid.Empty) {
            return Result.Failure<CycleDayModel>(
                Errors.Validation.Invalid(nameof(command.CycleId), "Cycle id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<CycleDayModel>(accessError);
        }

        var cycleId = new CycleId(command.CycleId);

        var cycle = await cycleRepository.GetByIdAsync(
            cycleId,
            userId,
            includeDays: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (cycle is null) {
            return Result.Failure<CycleDayModel>(Errors.Cycle.NotFound(command.CycleId));
        }

        var symptoms = command.Symptoms.ToValueObject();
        var day = cycle.AddOrUpdateDay(command.Date, command.IsPeriod, symptoms, command.Notes, command.ClearNotes);

        await cycleRepository.UpdateAsync(cycle, cancellationToken);
        return Result.Success(day.ToModel());
    }
}
