using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.SetAdminUserPassword;

public sealed record SetAdminUserPasswordCommand(
    Guid UserId,
    Guid ActorUserId,
    string NewPassword)
    : ICommand<Result>;
