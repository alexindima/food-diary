using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects;

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
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CycleDayModel>(accessError);
        }

        var cycleId = new CycleId(command.CycleId);

        Cycle? cycle = await cycleRepository.GetByIdAsync(
            cycleId,
            userId,
            includeDays: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (cycle is null) {
            return Result.Failure<CycleDayModel>(Errors.Cycle.NotFound(command.CycleId));
        }

        DailySymptoms symptoms = command.Symptoms.ToValueObject();
        CycleDay day = cycle.AddOrUpdateDay(command.Date, command.IsPeriod, symptoms, command.Notes, command.ClearNotes);

        await cycleRepository.UpdateAsync(cycle, cancellationToken).ConfigureAwait(false);
        return Result.Success(day.ToModel());
    }
}
