using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Contracts.Cycles;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public class CreateCycleCommandHandler(ICycleRepository cycleRepository)
    : ICommandHandler<CreateCycleCommand, Result<CycleResponse>>
{
    public async Task<Result<CycleResponse>> Handle(
        CreateCycleCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null)
        {
            return Result.Failure<CycleResponse>(Errors.User.NotFound());
        }

        var cycle = Cycle.Create(
            command.UserId.Value,
            command.StartDate,
            command.AverageLength,
            command.LutealLength,
            command.Notes);

        cycle = await cycleRepository.AddAsync(cycle, cancellationToken);

        var predictions = CyclePredictionService.CalculatePredictions(cycle);
        return Result.Success(cycle.ToResponse(predictions));
    }
}
