using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Commands.ChangePassword;

public record ChangePasswordCommand(
    Guid? UserId,
    string CurrentPassword,
    string NewPassword
) : ICommand<Result>, IUserRequest;
