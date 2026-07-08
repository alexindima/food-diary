using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Commands.DeleteUser;

public record DeleteUserCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
