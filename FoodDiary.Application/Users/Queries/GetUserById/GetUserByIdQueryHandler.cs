using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserByIdQuery, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == Guid.Empty) {
            return Result.Failure<UserModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId);
        return user is null
            ? Result.Failure<UserModel>(User.NotFound(userId))
            : Result.Success(user.ToModel());
    }
}
