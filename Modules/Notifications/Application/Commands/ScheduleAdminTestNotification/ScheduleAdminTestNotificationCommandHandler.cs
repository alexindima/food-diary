using System.Globalization;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Audit.Contracts.Common.Abstractions.Audit;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Commands.ScheduleAdminTestNotification;

public sealed class ScheduleAdminTestNotificationCommandHandler(
    IUserNotificationProfileService userProfileService,
    INotificationTestScheduler notificationTestScheduler,
    IAuditLogger auditLogger)
    : ICommandHandler<ScheduleAdminTestNotificationCommand, Result<ScheduledNotificationModel>> {
    public async Task<Result<ScheduledNotificationModel>> Handle(
        ScheduleAdminTestNotificationCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> actorResult = UserIdParser.Parse(command.ActorUserId);
        if (actorResult.IsFailure) {
            return UserIdParser.ToFailure<ScheduledNotificationModel>(actorResult);
        }

        Result<UserId> targetResult = UserIdParser.Parse(command.TargetUserId);
        if (targetResult.IsFailure) {
            return UserIdParser.ToFailure<ScheduledNotificationModel>(targetResult);
        }

        Result<UserNotificationProfileModel> profileResult = await userProfileService
            .GetAsync(targetResult.Value, cancellationToken).ConfigureAwait(false);
        if (profileResult.IsFailure) {
            // A target access failure must not invalidate the administrator's session.
            return Result.Failure<ScheduledNotificationModel>(UserErrors.NotFound(command.TargetUserId));
        }

        Result<ScheduledNotificationData> scheduledResult = await notificationTestScheduler.ScheduleAsync(
            command.TargetUserId, command.DelaySeconds, command.Type, cancellationToken).ConfigureAwait(false);
        if (scheduledResult.IsFailure) {
            return Result.Failure<ScheduledNotificationModel>(scheduledResult.Error);
        }

        ScheduledNotificationData scheduled = scheduledResult.Value;
        auditLogger.Log(
            "admin.notifications.test.scheduled",
            actorResult.Value,
            "User",
            command.TargetUserId.ToString(),
            string.Create(CultureInfo.InvariantCulture,
                $"type={scheduled.Type};delaySeconds={scheduled.DelaySeconds};scheduledAtUtc={scheduled.ScheduledAtUtc:O}"));

        return Result.Success(new ScheduledNotificationModel(scheduled.Type, scheduled.DelaySeconds, scheduled.ScheduledAtUtc));
    }
}
