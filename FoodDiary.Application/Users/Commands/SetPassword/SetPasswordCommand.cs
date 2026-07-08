using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Commands.SetPassword;

public sealed record SetPasswordCommand(
    Guid? UserId,
    string NewPassword
) : ICommand<Result>, IUserRequest;
