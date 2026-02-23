using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetDesiredWeight;

public class GetDesiredWeightQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetDesiredWeightQuery, Result<UserDesiredWeightResponse>> {
    public async Task<Result<UserDesiredWeightResponse>> Handle(
        GetDesiredWeightQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == UserId.Empty) {
            return Result.Failure<UserDesiredWeightResponse>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(query.UserId.Value);
        return user is null
            ? Result.Failure<UserDesiredWeightResponse>(Errors.User.NotFound(query.UserId.Value))
            : Result.Success(new UserDesiredWeightResponse(user.DesiredWeight));
    }
}
