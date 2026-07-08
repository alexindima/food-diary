using FoodDiary.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationPreferencesService(INotificationUserAccessService notificationUserAccessService) : INotificationPreferencesService {
    public async Task<Result<NotificationPreferencesModel>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<User> userResult = await notificationUserAccessService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<NotificationPreferencesModel>(userResult.Error);
        }

        return Result.Success(ToModel(userResult.Value));
    }

    public async Task<Result<NotificationPreferencesUpdateResult>> UpdateAsync(
        UserId userId,
        UserPreferenceUpdate update,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await notificationUserAccessService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<NotificationPreferencesUpdateResult>(userResult.Error);
        }

        User user = userResult.Value;
        user.UpdatePreferences(update);
        await notificationUserAccessService.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(new NotificationPreferencesUpdateResult(user.Id, ToModel(user)));
    }

    private static NotificationPreferencesModel ToModel(User user) =>
        new(
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours);
}
