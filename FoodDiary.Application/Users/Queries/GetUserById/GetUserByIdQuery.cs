using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetUserById;

public record GetUserByIdQuery(
    Guid? UserId
) : IQuery<Result<UserModel>>, IUserRequest;
