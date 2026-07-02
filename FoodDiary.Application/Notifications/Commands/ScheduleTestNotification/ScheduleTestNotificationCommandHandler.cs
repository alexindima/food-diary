using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.ScheduleTestNotification;

public sealed class ScheduleTestNotificationCommandHandler(
    INotificationTestScheduler notificationTestScheduler,
    ICurrentUserAccessService currentUserAccessService,
    IAuditLogger auditLogger)
    : ICommandHandler<ScheduleTestNotificationCommand, Result<ScheduledNotificationModel>> {
    public async Task<Result<ScheduledNotificationModel>> Handle(
        ScheduleTestNotificationCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ScheduledNotificationModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<ScheduledNotificationModel>(accessError);
        }

        ScheduledNotificationData scheduled = await notificationTestScheduler.ScheduleAsync(
            userId.Value,
            command.DelaySeconds,
            command.Type,
            cancellationToken).ConfigureAwait(false);

        auditLogger.Log(
            "notifications.test.scheduled",
            userId,
            "Notification",
            scheduled.Type,
            string.Create(CultureInfo.InvariantCulture, $"delaySeconds={scheduled.DelaySeconds};scheduledAtUtc={scheduled.ScheduledAtUtc:O}"));

        return Result.Success(new ScheduledNotificationModel(
            scheduled.Type,
            scheduled.DelaySeconds,
            scheduled.ScheduledAtUtc));
    }
}
