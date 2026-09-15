using WebPush;

namespace FoodDiary.Modules.Notifications.Infrastructure.Services;

public interface IWebPushClientAdapter {
    Task SendNotificationAsync(
        PushSubscription subscription,
        string payload,
        VapidDetails vapidDetails,
        CancellationToken cancellationToken);
}
