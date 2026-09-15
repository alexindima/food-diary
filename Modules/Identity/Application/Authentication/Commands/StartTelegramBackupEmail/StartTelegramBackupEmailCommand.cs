using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.StartTelegramBackupEmail;

public sealed record StartTelegramBackupEmailCommand(Guid? UserId, string Email, string BrowserBinding)
    : ICommand<Result<TelegramOidcStartModel>>, IUserRequest;
