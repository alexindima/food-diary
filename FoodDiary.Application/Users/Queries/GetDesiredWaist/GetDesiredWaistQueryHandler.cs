using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetDesiredWaist;

public class GetDesiredWaistQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetDesiredWaistQuery, Result<UserDesiredWaistResponse>> {
    public async Task<Result<UserDesiredWaistResponse>> Handle(
        GetDesiredWaistQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == UserId.Empty) {
            return Result.Failure<UserDesiredWaistResponse>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(query.UserId.Value);
        return user is null
            ? Result.Failure<UserDesiredWaistResponse>(Errors.User.NotFound(query.UserId.Value))
            : Result.Success(new UserDesiredWaistResponse(user.DesiredWaist));
    }
}
