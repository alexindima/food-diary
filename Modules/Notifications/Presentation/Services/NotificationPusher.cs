using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Modules.Notifications.Presentation.Services;

public sealed class NotificationPusher(IHubContext<NotificationHub> hubContext)
    : INotificationPusher {
    public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default) {
        return hubContext.Clients.User(userId.ToString())
            .SendAsync(NotificationHubMethods.UnreadCountUpdated, count, cancellationToken);
    }

    public Task PushNotificationsChangedAsync(Guid userId, CancellationToken cancellationToken = default) {
        return hubContext.Clients.User(userId.ToString())
            .SendAsync(NotificationHubMethods.NotificationsChanged, cancellationToken);
    }
}
