using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;

namespace FoodDiary.Application.Cycles.Queries.GetCurrentCycle;

public class GetCurrentCycleQueryHandler(ICycleRepository cycleRepository)
    : IQueryHandler<GetCurrentCycleQuery, Result<CycleModel?>> {
    public async Task<Result<CycleModel?>> Handle(
        GetCurrentCycleQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<CycleModel?>(userIdResult.Error);
        }

        var userId = userIdResult.Value;

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
