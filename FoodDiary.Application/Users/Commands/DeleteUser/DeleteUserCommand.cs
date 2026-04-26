using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Users.Commands.DeleteUser;

public record DeleteUserCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
