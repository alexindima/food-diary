using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Queries.GetDesiredWaist;

public sealed class GetDesiredWaistQueryHandler(IUserContextService userContextService)
    : IQueryHandler<GetDesiredWaistQuery, Result<UserDesiredWaistModel>> {
    public async Task<Result<UserDesiredWaistModel>> Handle(
        GetDesiredWaistQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<UserDesiredWaistModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserDesiredWaistModel>(userResult.Error)
            : Result.Success(new UserDesiredWaistModel(userResult.Value.DesiredWaist));
    }
}
