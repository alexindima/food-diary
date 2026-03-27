using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Cycles.Common;
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
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<CycleModel?>(Errors.User.NotFound());
        }

        var userId = new UserId(query.UserId.Value);

        var cycle = await cycleRepository.GetLatestAsync(
            userId,
            includeDays: true,
            cancellationToken: cancellationToken);

        if (cycle is null) {
            return Result.Success<CycleModel?>(null);
        }

        var predictions = CyclePredictionService.CalculatePredictions(cycle);
        return Result.Success<CycleModel?>(cycle.ToModel(predictions));
    }
}
