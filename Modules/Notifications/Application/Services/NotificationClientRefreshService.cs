using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

internal sealed class NotificationClientRefreshService(
    INotificationReadModelRepository notificationReadModelRepository,
    INotificationPusher notificationPusher) : INotificationClientRefreshService {
    public async Task RefreshAsync(
        UserId userId,
        bool pushChanged,
        CancellationToken cancellationToken) {
        int unreadCount = await notificationReadModelRepository
            .GetUnreadCountAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        await notificationPusher
            .PushUnreadCountAsync(userId.Value, unreadCount, cancellationToken)
            .ConfigureAwait(false);

        if (pushChanged) {
            await notificationPusher
                .PushNotificationsChangedAsync(userId.Value, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
