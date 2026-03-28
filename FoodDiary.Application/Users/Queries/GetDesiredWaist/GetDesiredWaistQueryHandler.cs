using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetDesiredWaist;

public class GetDesiredWaistQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetDesiredWaistQuery, Result<UserDesiredWaistModel>> {
    public async Task<Result<UserDesiredWaistModel>> Handle(
        GetDesiredWaistQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<UserDesiredWaistModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? Result.Failure<UserDesiredWaistModel>(Errors.User.NotFound(userId))
            : Result.Success(new UserDesiredWaistModel(user.DesiredWaist));
    }
}
