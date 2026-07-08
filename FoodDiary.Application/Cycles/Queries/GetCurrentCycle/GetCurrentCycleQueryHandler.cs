using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Queries.GetCurrentCycle;

public sealed class GetCurrentCycleQueryHandler(
    ICycleReadService cycleReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetCurrentCycleQuery, Result<CycleModel?>> {
    public async Task<Result<CycleModel?>> Handle(
        GetCurrentCycleQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<CycleModel?>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        CycleModel? cycle = await cycleReadService.GetCurrentAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(cycle);
    }
}
