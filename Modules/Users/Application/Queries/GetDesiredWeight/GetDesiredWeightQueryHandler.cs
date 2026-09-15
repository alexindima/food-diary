using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Queries.GetDesiredWeight;

public sealed class GetDesiredWeightQueryHandler(
    IUserProfileReadService userProfileReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetDesiredWeightQuery, Result<UserDesiredWeightModel>> {
    public async Task<Result<UserDesiredWeightModel>> Handle(
        GetDesiredWeightQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<UserDesiredWeightModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await userProfileReadService.GetDesiredWeightAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
