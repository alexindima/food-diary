using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Contracts.Cycles;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public class UpsertCycleDayCommandHandler(ICycleRepository cycleRepository)
    : ICommandHandler<UpsertCycleDayCommand, Result<CycleDayResponse>>
{
    public async Task<Result<CycleDayResponse>> Handle(
        UpsertCycleDayCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null)
        {
            return Result.Failure<CycleDayResponse>(Errors.User.NotFound());
        }

        var cycle = await cycleRepository.GetByIdAsync(
            command.CycleId,
            command.UserId.Value,
            includeDays: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (cycle is null)
        {
            return Result.Failure<CycleDayResponse>(Errors.Cycle.NotFound(command.CycleId.Value));
        }

        var symptoms = command.Symptoms.ToValueObject();
        var day = cycle.AddOrUpdateDay(command.Date, command.IsPeriod, symptoms, command.Notes);

        await cycleRepository.UpdateAsync(cycle, cancellationToken);
        return Result.Success(day.ToResponse());
    }
}
