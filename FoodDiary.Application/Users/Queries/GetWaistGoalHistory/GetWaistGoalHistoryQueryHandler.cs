using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Queries.GetWaistGoalHistory;

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
