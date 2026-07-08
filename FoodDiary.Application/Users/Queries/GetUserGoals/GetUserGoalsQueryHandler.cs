using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetUserGoals;

public sealed class GetUserGoalsQueryHandler(IUserProfileReadService userProfileReadService) : IQueryHandler<GetUserGoalsQuery, Result<GoalsModel>> {
    public async Task<Result<GoalsModel>> Handle(GetUserGoalsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<GoalsModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await userProfileReadService.GetGoalsAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
