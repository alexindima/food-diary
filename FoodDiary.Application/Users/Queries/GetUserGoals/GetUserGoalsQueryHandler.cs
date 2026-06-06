using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Queries.GetUserGoals;

public class GetUserGoalsQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserGoalsQuery, Result<GoalsModel>> {
    public async Task<Result<GoalsModel>> Handle(GetUserGoalsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<GoalsModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return accessError is not null
            ? Result.Failure<GoalsModel>(accessError)
            : Result.Success(user!.ToGoalsModel());
    }
}
