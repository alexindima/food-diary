using WebPush;

namespace FoodDiary.Integrations.Services;

internal sealed class WebPushClientAdapter : IWebPushClientAdapter {
    private readonly Func<PushSubscription, string, VapidDetails, CancellationToken, Task> _sendNotificationAsync;

    public WebPushClientAdapter() : this(SendWithWebPushClientAsync) {
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

    private static Task SendWithWebPushClientAsync(
        PushSubscription subscription,
        string payload,
        VapidDetails vapidDetails,
        CancellationToken cancellationToken) {
        var client = new WebPushClient();
        return client.SendNotificationAsync(subscription, payload, vapidDetails, cancellationToken);
    }
}
