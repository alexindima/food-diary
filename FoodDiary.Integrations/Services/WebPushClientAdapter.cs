using WebPush;

namespace FoodDiary.Integrations.Services;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class WebPushClientAdapter : IWebPushClientAdapter {
    private readonly Func<PushSubscription, string, VapidDetails, CancellationToken, Task> _sendNotificationAsync;

    public WebPushClientAdapter(HttpClient httpClient)
        : this((subscription, payload, vapidDetails, cancellationToken) =>
            SendWithWebPushClientAsync(httpClient, subscription, payload, vapidDetails, cancellationToken)) {
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

    private static async Task SendWithWebPushClientAsync(
        HttpClient httpClient,
        PushSubscription subscription,
        string payload,
        VapidDetails vapidDetails,
        CancellationToken cancellationToken) {
        using var client = new WebPushClient(httpClient);
        await client.SendNotificationAsync(subscription, payload, vapidDetails, cancellationToken).ConfigureAwait(false);
    }
}
