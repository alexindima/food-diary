using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.StartTelegramBackupEmail;

public sealed class StartTelegramBackupEmailCommandHandler(TelegramBackupEmailOidcService service)
    : ICommandHandler<StartTelegramBackupEmailCommand, Result<TelegramOidcStartModel>> {
    public Task<Result<TelegramOidcStartModel>> Handle(StartTelegramBackupEmailCommand command, CancellationToken cancellationToken) =>
        service.StartAsync(command.UserId ?? Guid.Empty, command.Email, command.BrowserBinding, cancellationToken);
}
