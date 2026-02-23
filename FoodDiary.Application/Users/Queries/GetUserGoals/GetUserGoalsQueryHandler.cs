using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Goals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetUserGoals;

public class GetUserGoalsQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserGoalsQuery, Result<GoalsResponse>> {
    public async Task<Result<GoalsResponse>> Handle(GetUserGoalsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == UserId.Empty) {
            return Result.Failure<GoalsResponse>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(query.UserId.Value);
        return user is null
            ? Result.Failure<GoalsResponse>(User.NotFound(query.UserId.Value))
            : Result.Success(user.ToGoalsResponse());
    }
}
