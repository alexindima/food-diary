using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserGoals;

public sealed class GetUserGoalsQueryHandler(
    IUserProfileReadService userProfileReadService,
    ICurrentUserAccessService currentUserAccessService) : IQueryHandler<GetUserGoalsQuery, Result<GoalsModel>> {
    public async Task<Result<GoalsModel>> Handle(GetUserGoalsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<GoalsModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await userProfileReadService.GetGoalsAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
