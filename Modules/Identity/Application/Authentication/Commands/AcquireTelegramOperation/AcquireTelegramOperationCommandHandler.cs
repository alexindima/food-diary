using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AcquireTelegramOperation;

public sealed class AcquireTelegramOperationCommandHandler(ITelegramOperationStore store, ITelegramOperationPolicy policy,
    IUserAuthenticationIdentityService identities, TimeProvider timeProvider) : ICommandHandler<AcquireTelegramOperationCommand, Result<TelegramOperationLease>> {
    public async Task<Result<TelegramOperationLease>> Handle(AcquireTelegramOperationCommand command, CancellationToken cancellationToken) {
        Guid operationId = command.OperationId;
        if (!TelegramOperationChecks.IsEnabled(policy)) {
            return Result.Failure<TelegramOperationLease>(TelegramOperationChecks.Unavailable);
        }
        TelegramOperationLease? lease = await store.AcquireAsync(policy.BotId, operationId, cancellationToken).ConfigureAwait(false);
        if (lease is null || !await TelegramOperationChecks.IsCurrentAsync(lease, store, policy, identities, timeProvider, cancellationToken).ConfigureAwait(false)) {
            return Result.Failure<TelegramOperationLease>(TelegramOperationChecks.Conflict);
        }
        return Result.Success(lease);
    }
}
