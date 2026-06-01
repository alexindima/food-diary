using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
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

        var user = await userRepository.GetByIdAsync(new UserId(command.UserId.Value), cancellationToken).ConfigureAwait(false);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<NotificationPreferencesModel>(accessError);
        }

        var currentUser = user!;
        var firstReminder = command.FastingCheckInReminderHours ?? currentUser.FastingCheckInReminderHours;
        var followUpReminder = command.FastingCheckInFollowUpReminderHours ?? currentUser.FastingCheckInFollowUpReminderHours;
        if (followUpReminder <= firstReminder) {
            return Result.Failure<NotificationPreferencesModel>(
                Errors.Validation.Invalid(
                    nameof(command.FastingCheckInFollowUpReminderHours),
                    "Follow-up reminder hour must be greater than the first reminder hour."));
        }

        currentUser.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: command.PushNotificationsEnabled,
            FastingPushNotificationsEnabled: command.FastingPushNotificationsEnabled,
            SocialPushNotificationsEnabled: command.SocialPushNotificationsEnabled,
            FastingCheckInReminderHours: command.FastingCheckInReminderHours,
            FastingCheckInFollowUpReminderHours: command.FastingCheckInFollowUpReminderHours));

        await userRepository.UpdateAsync(currentUser, cancellationToken).ConfigureAwait(false);
        auditLogger.Log(
            "notifications.preferences.updated",
            currentUser.Id,
            "User",
            currentUser.Id.Value.ToString(),
            $"push={currentUser.PushNotificationsEnabled};fasting={currentUser.FastingPushNotificationsEnabled};social={currentUser.SocialPushNotificationsEnabled};fastingReminder={currentUser.FastingCheckInReminderHours};fastingReminderFollowUp={currentUser.FastingCheckInFollowUpReminderHours}");

        return Result.Success(new NotificationPreferencesModel(
            currentUser.PushNotificationsEnabled,
            currentUser.FastingPushNotificationsEnabled,
            currentUser.SocialPushNotificationsEnabled,
            currentUser.FastingCheckInReminderHours,
            currentUser.FastingCheckInFollowUpReminderHours));
    }
}
