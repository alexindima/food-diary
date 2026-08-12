using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;
using FoodDiary.Application.Authentication.Common;
using Microsoft.Extensions.Logging;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Authentication.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    IEmailSender emailSender,
    TimeProvider dateTimeProvider,
    ILogger<RequestPasswordResetCommandHandler> logger)
    : ICommandHandler<RequestPasswordResetCommand, Result> {
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken) {
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        string token = SecurityTokenGenerator.GenerateUrlSafeToken();
        UserPasswordResetIssueModel issue = await userIdentityService
            .IssuePasswordResetAsync(
                command.Email,
                token,
                nowUtc.Add(TokenLifetime),
                nowUtc,
                Cooldown,
                cancellationToken)
            .ConfigureAwait(false);
        if (issue.Status == UserPasswordResetIssueStatus.Throttled) {
            logger.LogInformation("Password reset request throttled by cooldown.");
        }

        if (issue.Status != UserPasswordResetIssueStatus.Issued || issue.Delivery is null) {
            return Result.Success();
        }

        UserPasswordResetDeliveryModel delivery = issue.Delivery;
        PasswordResetMessage message = new(
            delivery.Email,
            delivery.UserId.ToString(),
            token,
            delivery.Language,
            command.ClientOrigin);
        await emailSender.SendPasswordResetAsync(message, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
