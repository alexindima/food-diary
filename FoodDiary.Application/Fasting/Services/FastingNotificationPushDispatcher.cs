using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingNotificationPushDispatcher {
    public static async Task PushAsync(
        IReadOnlyCollection<UserId> usersToPush,
        INotificationReadRepository notificationRepository,
        INotificationPusher notificationPusher,
        CancellationToken cancellationToken) {
        foreach (UserId userId in usersToPush) {
            int unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken).ConfigureAwait(false);
            await notificationPusher.PushUnreadCountAsync(userId.Value, unreadCount, cancellationToken).ConfigureAwait(false);
            await notificationPusher.PushNotificationsChangedAsync(userId.Value, cancellationToken).ConfigureAwait(false);
        }
    }
}
