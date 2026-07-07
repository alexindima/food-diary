using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Dashboard.Commands.SendDashboardTestEmail;

public sealed class SendDashboardTestEmailCommandHandler(
    IDashboardUserContextService dashboardUserContextService,
    IEmailSender emailSender,
    ILogger<SendDashboardTestEmailCommandHandler> logger) : ICommandHandler<SendDashboardTestEmailCommand, Result> {
    public async Task<Result> Handle(SendDashboardTestEmailCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        Result<User> userResult = await dashboardUserContextService
            .GetAccessibleUserAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        try {
            await emailSender.SendTestEmailAsync(new TestEmailMessage(userResult.Value.Email, userResult.Value.Language), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        } catch (Exception ex) {
            logger.LogWarning(ex, "Dashboard test email dispatch failed for user {UserId}.", command.UserId);
            return Result.Failure(
                Errors.Validation.Invalid(
                    "TestEmail",
                    "Failed to send test email."));
        }
    }
}
