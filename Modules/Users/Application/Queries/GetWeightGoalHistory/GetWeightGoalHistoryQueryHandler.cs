using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetWeightGoalHistory;

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
