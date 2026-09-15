using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Notifications.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using System.Globalization;

namespace FoodDiary.Modules.Notifications.Application.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandHandler(
    IUserNotificationProfileService userProfileService,
    IAuditLogger auditLogger,
    ICurrentUserAccessService notificationUserAccessService)
    : ICommandHandler<UpdateNotificationPreferencesCommand, Result<NotificationPreferencesModel>> {
    public async Task<Result<NotificationPreferencesModel>> Handle(
        UpdateNotificationPreferencesCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            notificationUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<NotificationPreferencesModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<NotificationPreferencesModel> currentPreferencesResult =
            await GetAsync(userId, cancellationToken).ConfigureAwait(false);
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

        Result<NotificationPreferencesUpdateResult> updateResult = await UpdateAsync(
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
    private async Task<Result<NotificationPreferencesModel>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserNotificationProfileModel> result = await userProfileService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            return Result.Failure<NotificationPreferencesModel>(result.Error);
        }

        return Result.Success(NotificationPreferenceMappings.ToModel(result.Value));
    }
    private async Task<Result<NotificationPreferencesUpdateResult>> UpdateAsync(
        UserId userId,
        UserPreferenceUpdate update,
        CancellationToken cancellationToken = default) {
        Result<UserNotificationProfileModel> result = await userProfileService
            .UpdatePreferencesAsync(userId, update, cancellationToken)
            .ConfigureAwait(false);
        if (result.IsFailure) {
            return Result.Failure<NotificationPreferencesUpdateResult>(result.Error);
        }

        return Result.Success(new NotificationPreferencesUpdateResult(result.Value.UserId, NotificationPreferenceMappings.ToModel(result.Value)));
    }
}
