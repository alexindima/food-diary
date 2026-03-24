using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetDesiredWeight;

public class GetDesiredWeightQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetDesiredWeightQuery, Result<UserDesiredWeightModel>> {
    public async Task<Result<UserDesiredWeightModel>> Handle(
        GetDesiredWeightQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == Guid.Empty) {
            return Result.Failure<UserDesiredWeightModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId);
        return user is null
            ? Result.Failure<UserDesiredWeightModel>(Errors.User.NotFound(userId))
            : Result.Success(new UserDesiredWeightModel(user.DesiredWeight));
    }
}
