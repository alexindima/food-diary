using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Globalization;

namespace FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandHandler(
    INotificationPreferencesService notificationPreferencesService,
    IAuditLogger auditLogger)
    : ICommandHandler<UpdateNotificationPreferencesCommand, Result<NotificationPreferencesModel>> {
    public async Task<Result<NotificationPreferencesModel>> Handle(
        UpdateNotificationPreferencesCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<NotificationPreferencesModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        Result<NotificationPreferencesModel> currentPreferencesResult =
            await notificationPreferencesService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (currentPreferencesResult.IsFailure) {
            return Result.Failure<NotificationPreferencesModel>(currentPreferencesResult.Error);
        }

        NotificationPreferencesModel currentPreferences = currentPreferencesResult.Value;
        int firstReminder = command.FastingCheckInReminderHours ?? currentPreferences.FastingCheckInReminderHours;
        int followUpReminder = command.FastingCheckInFollowUpReminderHours ?? currentPreferences.FastingCheckInFollowUpReminderHours;
        if (followUpReminder <= firstReminder) {
            return Result.Failure<NotificationPreferencesModel>(
                Errors.Validation.Invalid(
                    nameof(command.FastingCheckInFollowUpReminderHours),
                    "Follow-up reminder hour must be greater than the first reminder hour."));
        }

        var update = new UserPreferenceUpdate(
            PushNotificationsEnabled: command.PushNotificationsEnabled,
            FastingPushNotificationsEnabled: command.FastingPushNotificationsEnabled,
            SocialPushNotificationsEnabled: command.SocialPushNotificationsEnabled,
            FastingCheckInReminderHours: command.FastingCheckInReminderHours,
            FastingCheckInFollowUpReminderHours: command.FastingCheckInFollowUpReminderHours);

        Result<NotificationPreferencesUpdateResult> updateResult = await notificationPreferencesService.UpdateAsync(
            userId,
            update,
            cancellationToken).ConfigureAwait(false);

        if (updateResult.IsFailure) {
            return Result.Failure<NotificationPreferencesModel>(updateResult.Error);
        }

        NotificationPreferencesUpdateResult updated = updateResult.Value;
        NotificationPreferencesModel preferences = updated.Preferences;
        auditLogger.Log(
            "notifications.preferences.updated",
            updated.UserId,
            "User",
            updated.UserId.Value.ToString(),
            string.Create(CultureInfo.InvariantCulture, $"push={preferences.PushNotificationsEnabled};fasting={preferences.FastingPushNotificationsEnabled};social={preferences.SocialPushNotificationsEnabled};fastingReminder={preferences.FastingCheckInReminderHours};fastingReminderFollowUp={preferences.FastingCheckInFollowUpReminderHours}"));

        return Result.Success(preferences);
    }
}
