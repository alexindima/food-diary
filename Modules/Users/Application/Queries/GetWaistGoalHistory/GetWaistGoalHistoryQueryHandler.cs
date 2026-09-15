using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetWaistGoalHistory;

public sealed class GetWaistGoalHistoryQueryHandler(
    IUserProfileReadService userProfileReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWaistGoalHistoryQuery, Result<IReadOnlyList<WaistGoalHistoryModel>>> {
    public async Task<Result<IReadOnlyList<WaistGoalHistoryModel>>> Handle(
        GetWaistGoalHistoryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<WaistGoalHistoryModel>>(userIdResult);
        }

        return await userProfileReadService
            .GetWaistGoalHistoryAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}
