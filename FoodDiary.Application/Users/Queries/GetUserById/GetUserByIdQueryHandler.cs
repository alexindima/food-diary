using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserByIdQuery, Result<UserResponse>> {
    public async Task<Result<UserResponse>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId.Value == UserId.Empty) {
            return Result.Failure<UserResponse>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(query.UserId.Value);
        return user is null
            ? Result.Failure<UserResponse>(User.NotFound(query.UserId.Value))
            : Result.Success(user.ToResponse());
    }
}
