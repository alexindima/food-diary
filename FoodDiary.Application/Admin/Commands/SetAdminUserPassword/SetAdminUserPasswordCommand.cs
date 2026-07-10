using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.SetAdminUserPassword;

public sealed record SetAdminUserPasswordCommand(
    Guid UserId,
    string NewPassword)
    : ICommand<Result>;
