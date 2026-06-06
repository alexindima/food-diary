using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Cycles.Queries.GetCurrentCycle;

public class GetCurrentCycleQueryHandler(
    ICycleRepository cycleRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetCurrentCycleQuery, Result<CycleModel?>> {
    public async Task<Result<CycleModel?>> Handle(
        GetCurrentCycleQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<CycleModel?>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CycleModel?>(accessError);
        }

        Cycle? cycle = await cycleRepository.GetLatestAsync(
            userId,
            includeDays: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (cycle is null) {
            return Result.Success<CycleModel?>(value: null);
        }

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(cycle);
        return Result.Success<CycleModel?>(cycle.ToModel(predictions));
    }
}
