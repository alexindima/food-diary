using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetUserGoals;

public class GetUserGoalsQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserGoalsQuery, Result<GoalsModel>> {
    public async Task<Result<GoalsModel>> Handle(GetUserGoalsQuery query, CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<GoalsModel>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return accessError is not null
            ? Result.Failure<GoalsModel>(accessError)
            : Result.Success(user!.ToGoalsModel());
    }
}
