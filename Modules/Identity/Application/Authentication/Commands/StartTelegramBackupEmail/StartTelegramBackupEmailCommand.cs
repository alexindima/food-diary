using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.StartTelegramBackupEmail;

public sealed record StartTelegramBackupEmailCommand(Guid? UserId, string Email, string BrowserBinding)
    : ICommand<Result<TelegramOidcStartModel>>, IUserRequest;
