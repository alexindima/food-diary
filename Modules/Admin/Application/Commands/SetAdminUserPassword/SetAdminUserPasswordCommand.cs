using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.SetAdminUserPassword;

public sealed record SetAdminUserPasswordCommand(
    Guid UserId,
    Guid ActorUserId,
    string NewPassword)
    : ICommand<Result>;
