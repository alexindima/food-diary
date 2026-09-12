using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Identity.Authentication.Services;

namespace FoodDiary.Application.Identity.Authentication.Commands.CheckpointTelegramOperation;

public sealed class CheckpointTelegramOperationCommandHandler(TelegramOperationService service) : ICommandHandler<CheckpointTelegramOperationCommand, Result> {
    public Task<Result> Handle(CheckpointTelegramOperationCommand command, CancellationToken cancellationToken) =>
        service.CheckpointAsync(command.OperationId, command.LeaseId, command.Checkpoint, command.Completed, command.NextAttemptAtUtc, cancellationToken);
}
