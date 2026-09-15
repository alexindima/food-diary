using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CheckpointTelegramOperation;

public sealed class CheckpointTelegramOperationCommandHandler(ITelegramOperationStore store, ITelegramOperationPolicy policy,
    IUserAuthenticationIdentityService identities, TimeProvider timeProvider) : ICommandHandler<CheckpointTelegramOperationCommand, Result> {
    public async Task<Result> Handle(CheckpointTelegramOperationCommand command, CancellationToken cancellationToken) {
        Guid operationId = command.OperationId;
        Guid leaseId = command.LeaseId;
        string checkpoint = command.Checkpoint;
        bool completed = command.Completed;
        DateTime nextAttemptAtUtc = command.NextAttemptAtUtc;
        if (!TelegramOperationChecks.IsEnabled(policy)) {
            return Result.Failure(TelegramOperationChecks.Unavailable);
        }
        if (!TelegramOperationChecks.ValidPayload(checkpoint) || nextAttemptAtUtc.Kind != DateTimeKind.Utc || nextAttemptAtUtc > timeProvider.GetUtcNow().UtcDateTime.AddDays(1)) {
            return Result.Failure(TelegramOperationChecks.Invalid);
        }
        TelegramOperationLease? lease = await store.GetLeaseAsync(policy.BotId, operationId, leaseId, cancellationToken).ConfigureAwait(false);
        if (lease is null || !await TelegramOperationChecks.IsCurrentAsync(lease, store, policy, identities, timeProvider, cancellationToken).ConfigureAwait(false)) {
            return Result.Failure(TelegramOperationChecks.Conflict);
        }
        return await store.CheckpointAsync(policy.BotId, operationId, leaseId, checkpoint, completed, nextAttemptAtUtc,
            cancellationToken).ConfigureAwait(false) ? Result.Success() : Result.Failure(TelegramOperationChecks.Conflict);
    }
}
