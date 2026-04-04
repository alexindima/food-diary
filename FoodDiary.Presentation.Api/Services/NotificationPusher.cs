using FoodDiary.Application.Notifications.Common;
using FoodDiary.Presentation.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Presentation.Api.Services;

public sealed class NotificationPusher(IHubContext<NotificationHub> hubContext)
    : INotificationPusher {
    public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default) {
        return hubContext.Clients.User(userId.ToString())
            .SendAsync(NotificationHubMethods.UnreadCountUpdated, count, cancellationToken);
    }
}
