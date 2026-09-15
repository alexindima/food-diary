using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(
    IUserProfileReadService userProfileReadService,
    ICurrentUserAccessService currentUserAccessService) : IQueryHandler<GetUserByIdQuery, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<UserModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await userProfileReadService.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
