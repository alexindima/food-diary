using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetDesiredWaist;

public sealed class GetDesiredWaistQueryHandler(IUserProfileReadService userProfileReadService)
    : IQueryHandler<GetDesiredWaistQuery, Result<UserDesiredWaistModel>> {
    public async Task<Result<UserDesiredWaistModel>> Handle(
        GetDesiredWaistQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<UserDesiredWaistModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        return await userProfileReadService.GetDesiredWaistAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
