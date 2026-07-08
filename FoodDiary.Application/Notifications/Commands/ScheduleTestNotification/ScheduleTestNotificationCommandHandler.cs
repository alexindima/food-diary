using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
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
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ScheduledNotificationModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
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
