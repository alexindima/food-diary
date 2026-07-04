using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Cycles.Queries.GetCurrentCycle;

public sealed class GetCurrentCycleQueryHandler(
    ICycleReadRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetCurrentCycleQuery, Result<CycleModel?>> {
    public async Task<Result<CycleModel?>> Handle(
        GetCurrentCycleQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<CycleModel?>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CycleModel?>(accessError);
        }

        CycleProfile? profile = await cycleRepository.GetCurrentAsync(
            userId,
            includeDetails: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (profile is null) {
            return Result.Success<CycleModel?>(value: null);
        }

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);
        return Result.Success<CycleModel?>(profile.ToModel(predictions));
    }
}
