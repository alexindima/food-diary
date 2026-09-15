using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Services;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CheckpointTelegramOperation;

public sealed class CheckpointTelegramOperationCommandHandler(TelegramOperationService service) : ICommandHandler<CheckpointTelegramOperationCommand, Result> {
    public Task<Result> Handle(CheckpointTelegramOperationCommand command, CancellationToken cancellationToken) =>
        service.CheckpointAsync(command.OperationId, command.LeaseId, command.Checkpoint, command.Completed, command.NextAttemptAtUtc, cancellationToken);
}
