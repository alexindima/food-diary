using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.ResumeTelegramOperation;

public sealed class ResumeTelegramOperationCommandHandler(ITelegramOperationStore store, ITelegramOperationPolicy policy,
    IUserAuthenticationIdentityService identities, TimeProvider timeProvider) : ICommandHandler<ResumeTelegramOperationCommand, Result> {
    public async Task<Result> Handle(ResumeTelegramOperationCommand command, CancellationToken cancellationToken) {
        if (!TelegramOperationChecks.IsEnabled(policy)) {
            return Result.Failure(TelegramOperationChecks.Unavailable);
        }
        if (command.OperationId.Value == Guid.Empty || command.TelegramUserId <= 0 ||
            !TelegramOperationChecks.ValidPayload(command.ExpectedCheckpoint) || !TelegramOperationChecks.ValidPayload(command.Checkpoint) ||
            string.Equals(command.ExpectedCheckpoint, command.Checkpoint, StringComparison.Ordinal)) {
            return Result.Failure(TelegramOperationChecks.Invalid);
        }
        Result<UserAuthenticationPrincipalModel> principal = await identities.AuthenticateTelegramAsync(command.TelegramUserId,
            timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (principal.IsFailure) {
            return Result.Failure(TelegramOperationChecks.Conflict);
        }
        bool resumed = await store.ResumeAsync(policy.BotId, command.OperationId, principal.Value.UserId, principal.Value.SecurityVersion,
            command.ExpectedCheckpoint, command.Checkpoint, cancellationToken);
        return resumed ? Result.Success() : Result.Failure(TelegramOperationChecks.Conflict);
    }
}
