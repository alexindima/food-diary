using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.RequestTelegramBackupEmail;

public sealed class RequestTelegramBackupEmailCommandHandler(TelegramBackupEmailService service)
    : ICommandHandler<RequestTelegramBackupEmailCommand, Result> {
    public Task<Result> Handle(RequestTelegramBackupEmailCommand command, CancellationToken cancellationToken) =>
        service.RequestAsync(command.UserId ?? Guid.Empty, command.Email, command.InitData, cancellationToken);
}
