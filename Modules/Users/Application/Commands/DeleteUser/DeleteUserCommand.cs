using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.DeleteUser;

public record DeleteUserCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
