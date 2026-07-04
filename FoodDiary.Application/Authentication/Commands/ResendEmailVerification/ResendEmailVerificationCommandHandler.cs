using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public class ResendEmailVerificationCommandHandler(
    IUserContextService userContextService,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    TimeProvider dateTimeProvider,
    IPostCommitActionQueue postCommitActionQueue,
    ILogger<ResendEmailVerificationCommandHandler> logger) : ICommandHandler<ResendEmailVerificationCommand, Result> {
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

    public async Task<Result> Handle(ResendEmailVerificationCommand command, CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

        Result<User> userResult = await userContextService
            .GetAccessibleUserAsync(new UserId(command.UserId), cancellationToken)
            .ConfigureAwait(false);
        if (!userResult.IsSuccess) {
            return Result.Failure(userResult.Error);
        }

        User currentUser = userResult.Value;
        if (currentUser.IsEmailConfirmed) {
            return Result.Success();
        }

        if (currentUser.EmailConfirmationSentAtUtc.HasValue) {
            TimeSpan elapsed = dateTimeProvider.GetUtcNow().UtcDateTime - currentUser.EmailConfirmationSentAtUtc.Value;
            if (elapsed < ResendCooldown) {
                return Result.Failure(
                    Errors.Validation.Invalid(
                        "EmailVerification",
                        "Verification email was sent recently. Please wait before requesting a new one."));
            }
        }

        string emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        string emailTokenHash = passwordHasher.Hash(emailToken);
        currentUser.SetEmailConfirmationToken(new UserTokenIssue(
            TokenHash: emailTokenHash,
            ExpiresAtUtc: dateTimeProvider.GetUtcNow().UtcDateTime.AddHours(24),
            IssuedAtUtc: dateTimeProvider.GetUtcNow().UtcDateTime));
        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

        EmailVerificationMessage message = new(currentUser.Email, currentUser.Id.Value.ToString(), emailToken, currentUser.Language, command.ClientOrigin);
        postCommitActionQueue.Enqueue(async ct => {
            try {
                await emailSender.SendEmailVerificationAsync(message, ct).ConfigureAwait(false);
            } catch (Exception ex) {
                logger.LogError(ex, "Email verification dispatch failed.");
            }
        });

        return Result.Success();
    }
}
