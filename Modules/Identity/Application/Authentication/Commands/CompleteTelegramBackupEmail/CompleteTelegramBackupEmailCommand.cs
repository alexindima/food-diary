using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CompleteTelegramBackupEmail;

public sealed record CompleteTelegramBackupEmailCommand(Guid? UserId, string Code, string State, string BrowserBinding)
    : ICommand<Result>, IUserRequest;
