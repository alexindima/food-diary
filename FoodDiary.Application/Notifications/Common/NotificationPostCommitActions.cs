using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

internal static class NotificationPostCommitActions {
    public static void EnqueueUnreadCountPush(
        IPostCommitActionQueue postCommitActionQueue,
        INotificationRepository notificationRepository,
        INotificationPusher notificationPusher,
        UserId userId,
        bool pushChanged = true) {
        postCommitActionQueue.Enqueue(async cancellationToken => {
            int unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken).ConfigureAwait(false);
            await notificationPusher.PushUnreadCountAsync(userId.Value, unreadCount, cancellationToken).ConfigureAwait(false);

            if (pushChanged) {
                await notificationPusher.PushNotificationsChangedAsync(userId.Value, cancellationToken).ConfigureAwait(false);
            }
        });
    }
}
