using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.CompleteTelegramBackupEmail;

public sealed class CompleteTelegramBackupEmailCommandHandler(TelegramBackupEmailOidcService service)
    : ICommandHandler<CompleteTelegramBackupEmailCommand, Result> {
    public Task<Result> Handle(CompleteTelegramBackupEmailCommand command, CancellationToken cancellationToken) =>
        service.CompleteAsync(command.UserId ?? Guid.Empty, command.Code, command.State, command.BrowserBinding, cancellationToken);
}
