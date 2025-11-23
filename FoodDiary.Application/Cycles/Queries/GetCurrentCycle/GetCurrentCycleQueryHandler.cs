using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Contracts.Cycles;

namespace FoodDiary.Application.Cycles.Queries.GetCurrentCycle;

public class GetCurrentCycleQueryHandler(ICycleRepository cycleRepository)
    : IQueryHandler<GetCurrentCycleQuery, Result<CycleResponse?>>
{
    public async Task<Result<CycleResponse?>> Handle(
        GetCurrentCycleQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<CycleResponse?>(Errors.User.NotFound());
        }

        var cycle = await cycleRepository.GetLatestAsync(
            query.UserId.Value,
            includeDays: true,
            cancellationToken: cancellationToken);

        if (cycle is null)
        {
            return Result.Success<CycleResponse?>(null);
        }

        var predictions = CyclePredictionService.CalculatePredictions(cycle);
        return Result.Success<CycleResponse?>(cycle.ToResponse(predictions));
    }
}
