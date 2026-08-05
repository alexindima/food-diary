using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Queries.GetWeightGoalHistory;

public sealed class GetWeightGoalHistoryQueryHandler(
    IUserProfileReadService userProfileReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetWeightGoalHistoryQuery, Result<IReadOnlyList<WeightGoalHistoryModel>>> {
    public async Task<Result<IReadOnlyList<WeightGoalHistoryModel>>> Handle(
        GetWeightGoalHistoryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<WeightGoalHistoryModel>>(userIdResult);
        }

        return await userProfileReadService
            .GetWeightGoalHistoryAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}
