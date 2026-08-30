using WebPush;

namespace FoodDiary.Integrations.Services;

public interface IWebPushClientAdapter {
    Task SendNotificationAsync(
        PushSubscription subscription,
        string payload,
        VapidDetails vapidDetails,
        CancellationToken cancellationToken);
}
