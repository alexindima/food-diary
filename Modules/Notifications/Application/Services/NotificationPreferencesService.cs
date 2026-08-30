using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationPreferencesService(IUserNotificationProfileService userProfileService) : INotificationPreferencesService {
    public async Task<Result<NotificationPreferencesModel>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserNotificationProfileModel> result = await userProfileService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            return Result.Failure<NotificationPreferencesModel>(result.Error);
        }

        return Result.Success(ToModel(result.Value));
    }

    public async Task<Result<NotificationPreferencesUpdateResult>> UpdateAsync(
        UserId userId,
        UserPreferenceUpdate update,
        CancellationToken cancellationToken = default) {
        Result<UserNotificationProfileModel> result = await userProfileService
            .UpdatePreferencesAsync(userId, update, cancellationToken)
            .ConfigureAwait(false);
        if (result.IsFailure) {
            return Result.Failure<NotificationPreferencesUpdateResult>(result.Error);
        }

        return Result.Success(new NotificationPreferencesUpdateResult(result.Value.UserId, ToModel(result.Value)));
    }

    private static NotificationPreferencesModel ToModel(UserNotificationProfileModel user) =>
        new(
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours);
}
