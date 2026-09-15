using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserById;

public record GetUserByIdQuery(
    Guid? UserId
) : IQuery<Result<UserModel>>, IUserRequest;
