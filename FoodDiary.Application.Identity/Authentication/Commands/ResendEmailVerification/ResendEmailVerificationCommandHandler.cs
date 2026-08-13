using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public sealed class ResendEmailVerificationCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
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

        string emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        string emailTokenHash = passwordHasher.Hash(emailToken);
        DateTime issuedAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        Result<UserEmailVerificationDeliveryModel?> issueResult = await userIdentityService
            .IssueEmailVerificationAsync(
                userIdResult.Value,
                emailTokenHash,
                issuedAtUtc.AddHours(24),
                issuedAtUtc,
                ResendCooldown,
                cancellationToken)
            .ConfigureAwait(false);
        if (issueResult.IsFailure) {
            return Result.Failure(issueResult.Error);
        }

        UserEmailVerificationDeliveryModel? delivery = issueResult.Value;
        if (delivery is null) {
            return Result.Success();
        }

        EmailVerificationMessage message = new(delivery.Email, delivery.UserId.ToString(), emailToken, delivery.Language, command.ClientOrigin);
        await emailSender.SendEmailVerificationAsync(message, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
