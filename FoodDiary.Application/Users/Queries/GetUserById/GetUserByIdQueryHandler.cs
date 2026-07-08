using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetUserById;

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
