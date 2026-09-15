using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RequestTelegramBackupEmail;

public sealed class RequestTelegramBackupEmailCommandHandler(TelegramBackupEmailService service)
    : ICommandHandler<RequestTelegramBackupEmailCommand, Result> {
    public Task<Result> Handle(RequestTelegramBackupEmailCommand command, CancellationToken cancellationToken) =>
        service.RequestAsync(command.UserId ?? Guid.Empty, command.Email, command.InitData, cancellationToken);
}
