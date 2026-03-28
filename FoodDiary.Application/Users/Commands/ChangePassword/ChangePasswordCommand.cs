using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Users.Commands.ChangePassword;

public record ChangePasswordCommand(
    Guid? UserId,
    string CurrentPassword,
    string NewPassword
) : ICommand<Result<bool>>, IUserRequest;
