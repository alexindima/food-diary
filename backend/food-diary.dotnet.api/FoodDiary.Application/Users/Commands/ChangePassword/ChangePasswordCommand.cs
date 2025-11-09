using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.ChangePassword;

public record ChangePasswordCommand(
    UserId? UserId,
    string CurrentPassword,
    string NewPassword
) : ICommand<Result<bool>>;
