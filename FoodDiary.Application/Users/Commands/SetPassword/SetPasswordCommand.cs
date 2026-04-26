using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Users.Commands.SetPassword;

public sealed record SetPasswordCommand(
    Guid? UserId,
    string NewPassword
) : ICommand<Result>, IUserRequest;
