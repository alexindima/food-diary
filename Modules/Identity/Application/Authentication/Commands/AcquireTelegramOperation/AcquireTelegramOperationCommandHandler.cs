using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Identity.Authentication.Services;

namespace FoodDiary.Application.Identity.Authentication.Commands.AcquireTelegramOperation;

public sealed class AcquireTelegramOperationCommandHandler(TelegramOperationService service) : ICommandHandler<AcquireTelegramOperationCommand, Result<TelegramOperationLease>> {
    public Task<Result<TelegramOperationLease>> Handle(AcquireTelegramOperationCommand command, CancellationToken cancellationToken) =>
        service.AcquireAsync(command.OperationId, cancellationToken);
}
