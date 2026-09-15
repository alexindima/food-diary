using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    IPostCommitActionQueue postCommitActionQueue,
    IEmailVerificationNotifier emailVerificationNotifier,
    FoodDiary.Modules.Identity.Application.Authentication.Services.TelegramBackupEmailService? backupEmails = null)
    : ICommandHandler<VerifyEmailCommand, Result> {
    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        if (command.Token.StartsWith(FoodDiary.Modules.Identity.Application.Authentication.Services.TelegramBackupEmailService.TokenPrefix, StringComparison.Ordinal)) {
            return backupEmails is null
                ? Result.Failure(new Error("Authentication.TelegramProofRequired", "Request a new confirmation.", ErrorKind.Unauthorized))
                : await backupEmails.ConfirmAsync(userId.Value, command.Token, cancellationToken).ConfigureAwait(false);
        }
        Result<bool> verificationResult = await userIdentityService
            .VerifyEmailAsync(userId, command.Token, dateTimeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            .ConfigureAwait(false);
        if (verificationResult.IsFailure) {
            return Result.Failure(verificationResult.Error);
        }

        if (!verificationResult.Value) {
            return Result.Success();
        }

        postCommitActionQueue.Enqueue("auth.email-verification.hub-notify", async ct => {
            try {
                await emailVerificationNotifier.NotifyEmailVerifiedAsync(userId.Value, ct).ConfigureAwait(false);
            } catch {
                // Notification failures shouldn't block verification.
            }
        });

        return Result.Success();
    }
}
