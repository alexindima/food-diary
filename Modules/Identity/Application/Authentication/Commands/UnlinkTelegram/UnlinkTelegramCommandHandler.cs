using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

namespace FoodDiary.Application.Identity.Authentication.Commands.UnlinkTelegram;

public sealed class UnlinkTelegramCommandHandler(ITelegramAuthValidator validator, ITelegramAssertionReplayGuard replayGuard,
    IUserAuthenticationIdentityService identities, IUserTelegramAccountService accounts, TimeProvider timeProvider,
    ITelegramOperationStore operations, IPostCommitActionQueue postCommitActions)
    : ICommandHandler<UnlinkTelegramCommand, Result> {
    public async Task<Result> Handle(UnlinkTelegramCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty || string.IsNullOrWhiteSpace(command.InitData)) {
            return InvalidProof();
        }
        Result<TelegramInitData> proof = validator.ValidateInitData(command.InitData);
        if (proof.IsFailure) {
            return Result.Failure(proof.Error);
        }
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (proof.Value.AuthDateUtc.Kind != DateTimeKind.Utc || proof.Value.AuthDateUtc > now || proof.Value.AuthDateUtc <= now.AddMinutes(-5)) {
            return InvalidProof();
        }
        Result<UserAuthenticationPrincipalModel> principal = await identities.AuthenticateTelegramAsync(proof.Value.UserId, now, cancellationToken).ConfigureAwait(false);
        if (principal.IsFailure) {
            return Result.Failure(principal.Error);
        }
        if (principal.Value.UserId.Value != command.UserId) {
            return InvalidProof();
        }
        if (!await replayGuard.TryConsumeAsync(command.InitData, proof.Value.AuthDateUtc.AddDays(1), cancellationToken).ConfigureAwait(false)) {
            return InvalidProof();
        }
        Result result = await accounts.UnlinkAsync(principal.Value.UserId, proof.Value.UserId, principal.Value.SecurityVersion, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess) {
            postCommitActions.Enqueue("auth.telegram.unlink.cancel-operations",
                ct => operations.CancelUserAsync(principal.Value.UserId.Value, ct));
        }
        return result;
    }

    private static Result InvalidProof() => Result.Failure(new Error("Authentication.TelegramProofRequired",
        "Reopen the Telegram app to confirm this account action.", ErrorKind.Unauthorized));
}
