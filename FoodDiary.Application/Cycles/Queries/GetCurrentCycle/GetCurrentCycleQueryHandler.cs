using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Cycles.Queries.GetCurrentCycle;

public class GetCurrentCycleQueryHandler(
    ICycleRepository cycleRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetCurrentCycleQuery, Result<CycleModel?>> {
    public async Task<Result<CycleModel?>> Handle(
        GetCurrentCycleQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<CycleModel?>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<CycleModel?>(accessError);
        }

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
