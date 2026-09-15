using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Services;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RegisterTelegramOperation;

public sealed class RegisterTelegramOperationCommandHandler(TelegramOperationService service) : ICommandHandler<RegisterTelegramOperationCommand, Result<Guid>> {
    public Task<Result<Guid>> Handle(RegisterTelegramOperationCommand command, CancellationToken cancellationToken) =>
        service.RegisterAsync(command.UpdateId, command.TelegramUserId, command.Payload, cancellationToken);
}
