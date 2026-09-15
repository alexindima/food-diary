using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.ChangePassword;

public record ChangePasswordCommand(
    Guid? UserId,
    string CurrentPassword,
    string NewPassword
) : ICommand<Result>, IUserRequest;
