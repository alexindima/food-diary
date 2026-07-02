using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationPreferencesService(IUserRepository userRepository) : INotificationPreferencesService {
    public async Task<Result<NotificationPreferencesModel>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<NotificationPreferencesModel>(accessError);
        }

        return Result.Success(ToModel(user!));
    }

    public async Task<Result<NotificationPreferencesUpdateResult>> UpdateAsync(
        UserId userId,
        UserPreferenceUpdate update,
        CancellationToken cancellationToken = default) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<NotificationPreferencesUpdateResult>(accessError);
        }

        user!.UpdatePreferences(update);
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
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
