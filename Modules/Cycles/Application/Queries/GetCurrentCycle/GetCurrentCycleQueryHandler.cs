using FoodDiary.Modules.Cycles.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Application.Abstractions.Models;
using FoodDiary.Modules.Cycles.Application.Mappings;
using FoodDiary.Modules.Cycles.Application.Services;
using FoodDiary.Modules.Cycles.Contracts.Queries.GetCurrentCycle;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Application.Queries.GetCurrentCycle;

public sealed class GetCurrentCycleQueryHandler(
    ICycleReadModelRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider timeProvider)
    : IQueryHandler<GetCurrentCycleQuery, Result<CycleModel?>> {
    public async Task<Result<CycleModel?>> Handle(
        GetCurrentCycleQuery query,
        CancellationToken cancellationToken) {
        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<CycleModel?>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        CycleProfileReadModel? profile = await cycleRepository.GetCurrentReadModelAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(profile?.ToModel(CyclePredictionService.CalculatePredictions(profile, currentDate: currentDate, timeProvider: timeProvider), currentDate));
    }
}
