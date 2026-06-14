using WebPush;

namespace FoodDiary.Integrations.Services;

internal sealed class WebPushClientAdapter : IWebPushClientAdapter {
    public async Task SendNotificationAsync(
        PushSubscription subscription,
        string payload,
        VapidDetails vapidDetails,
        CancellationToken cancellationToken) {
        var client = new WebPushClient();
        await client.SendNotificationAsync(subscription, payload, vapidDetails, cancellationToken).ConfigureAwait(false);
    }
}
