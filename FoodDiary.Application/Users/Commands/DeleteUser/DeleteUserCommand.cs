using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.DeleteUser;

public record DeleteUserCommand(
    UserId? UserId
) : ICommand<Result<bool>>;
