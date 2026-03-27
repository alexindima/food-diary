using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public class UpsertCycleDayCommandHandler(ICycleRepository cycleRepository)
    : ICommandHandler<UpsertCycleDayCommand, Result<CycleDayModel>> {
    public async Task<Result<CycleDayModel>> Handle(
        UpsertCycleDayCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CycleDayModel>(Errors.User.NotFound());
        }

        var userId = new UserId(command.UserId.Value);
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
        var day = cycle.AddOrUpdateDay(command.Date, command.IsPeriod, symptoms, command.Notes);

        await cycleRepository.UpdateAsync(cycle, cancellationToken);
        return Result.Success(day.ToModel());
    }
}
