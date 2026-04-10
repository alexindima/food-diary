using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandHandler(
    IUserRepository userRepository,
    IAuditLogger auditLogger)
    : ICommandHandler<UpdateNotificationPreferencesCommand, Result<NotificationPreferencesModel>> {
    public async Task<Result<NotificationPreferencesModel>> Handle(
        UpdateNotificationPreferencesCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<NotificationPreferencesModel>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(new UserId(command.UserId.Value), cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<NotificationPreferencesModel>(accessError);
        }

        user!.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: command.PushNotificationsEnabled,
            FastingPushNotificationsEnabled: command.FastingPushNotificationsEnabled,
            SocialPushNotificationsEnabled: command.SocialPushNotificationsEnabled,
            FastingCheckInReminderHours: command.FastingCheckInReminderHours,
            FastingCheckInFollowUpReminderHours: command.FastingCheckInFollowUpReminderHours));

        await userRepository.UpdateAsync(user, cancellationToken);
        auditLogger.Log(
            "notifications.preferences.updated",
            user.Id,
            "User",
            user.Id.Value.ToString(),
            $"push={user.PushNotificationsEnabled};fasting={user.FastingPushNotificationsEnabled};social={user.SocialPushNotificationsEnabled};fastingReminder={user.FastingCheckInReminderHours};fastingReminderFollowUp={user.FastingCheckInFollowUpReminderHours}");

        return Result.Success(new NotificationPreferencesModel(
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours));
    }
}
