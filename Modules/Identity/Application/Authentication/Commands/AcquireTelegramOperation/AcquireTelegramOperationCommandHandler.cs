using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Services;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AcquireTelegramOperation;

public sealed class AcquireTelegramOperationCommandHandler(TelegramOperationService service) : ICommandHandler<AcquireTelegramOperationCommand, Result<TelegramOperationLease>> {
    public Task<Result<TelegramOperationLease>> Handle(AcquireTelegramOperationCommand command, CancellationToken cancellationToken) =>
        service.AcquireAsync(command.OperationId, cancellationToken);
}
