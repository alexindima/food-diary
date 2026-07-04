using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(IUserContextService userContextService) : IQueryHandler<GetUserByIdQuery, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<UserModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserModel>(userResult.Error)
            : Result.Success(userResult.Value.ToModel());
    }
}
