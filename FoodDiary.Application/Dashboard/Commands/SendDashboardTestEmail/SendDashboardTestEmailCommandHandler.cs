using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Dashboard.Commands.SendDashboardTestEmail;

public sealed class SendDashboardTestEmailCommandHandler(
    IUserRepository userRepository,
    IEmailSender emailSender,
    ILogger<SendDashboardTestEmailCommandHandler> logger) : ICommandHandler<SendDashboardTestEmailCommand, Result> {
    public async Task<Result> Handle(SendDashboardTestEmailCommand command, CancellationToken cancellationToken) {
        User? user = await userRepository.GetByIdAsync(new UserId(command.UserId), cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        try {
            await emailSender.SendTestEmailAsync(new TestEmailMessage(user!.Email, user.Language), cancellationToken).ConfigureAwait(false);
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
