using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.SetPassword;

public sealed record SetPasswordCommand(
    Guid? UserId,
    string NewPassword
) : ICommand<Result>, IUserRequest;
