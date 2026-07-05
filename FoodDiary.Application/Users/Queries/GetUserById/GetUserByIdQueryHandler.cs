using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(IUserProfileReadService userProfileReadService) : IQueryHandler<GetUserByIdQuery, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<UserModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        return await userProfileReadService.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
