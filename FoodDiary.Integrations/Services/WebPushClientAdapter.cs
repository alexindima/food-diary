using WebPush;

namespace FoodDiary.Integrations.Services;

internal sealed class WebPushClientAdapter : IWebPushClientAdapter {
    private readonly Func<PushSubscription, string, VapidDetails, CancellationToken, Task> _sendNotificationAsync;

    public WebPushClientAdapter()
        : this(static async (subscription, payload, vapidDetails, cancellationToken) => {
            var client = new WebPushClient();
            await client.SendNotificationAsync(subscription, payload, vapidDetails, cancellationToken).ConfigureAwait(false);
        }) {
    }

    internal WebPushClientAdapter(
        Func<PushSubscription, string, VapidDetails, CancellationToken, Task> sendNotificationAsync) {
        _sendNotificationAsync = sendNotificationAsync;
    }

    public async Task SendNotificationAsync(
        PushSubscription subscription,
        string payload,
        VapidDetails vapidDetails,
        CancellationToken cancellationToken) {
        await _sendNotificationAsync(subscription, payload, vapidDetails, cancellationToken).ConfigureAwait(false);
    }
}
