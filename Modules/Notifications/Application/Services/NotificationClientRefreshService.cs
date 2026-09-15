using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Services;

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
