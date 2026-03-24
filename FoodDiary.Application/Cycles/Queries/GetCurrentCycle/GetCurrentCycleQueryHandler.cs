using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Queries.GetCurrentCycle;

public class GetCurrentCycleQueryHandler(ICycleRepository cycleRepository)
    : IQueryHandler<GetCurrentCycleQuery, Result<CycleModel?>> {
    public async Task<Result<CycleModel?>> Handle(
        GetCurrentCycleQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<CycleModel?>(Errors.User.NotFound());
        }

        var cycle = await cycleRepository.GetLatestAsync(
            query.UserId.Value,
            includeDays: true,
            cancellationToken: cancellationToken);

        if (cycle is null) {
            return Result.Success<CycleModel?>(null);
        }

        var predictions = CyclePredictionService.CalculatePredictions(cycle);
        return Result.Success<CycleModel?>(cycle.ToModel(predictions));
    }
}
