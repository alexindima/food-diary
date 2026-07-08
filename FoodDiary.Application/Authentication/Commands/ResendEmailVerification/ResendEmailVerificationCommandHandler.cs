using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public sealed class ResendEmailVerificationCommandHandler(
    IUserContextService userContextService,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    TimeProvider dateTimeProvider) : ICommandHandler<ResendEmailVerificationCommand, Result> {
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

    public async Task<Result> Handle(ResendEmailVerificationCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        Result<User> userResult = await userContextService
            .GetAccessibleUserAsync(userIdResult.Value, cancellationToken)
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
        await emailSender.SendEmailVerificationAsync(message, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
