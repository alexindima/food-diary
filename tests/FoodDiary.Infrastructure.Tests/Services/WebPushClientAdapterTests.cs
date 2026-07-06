using FoodDiary.Integrations.Services;
using WebPush;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class WebPushClientAdapterTests {
    [Fact]
    public async Task SendNotificationAsync_WhenClientCompletes_CompletesAndForwardsArguments() {
        PushSubscription? capturedSubscription = null;
        string? capturedPayload = null;
        VapidDetails? capturedVapidDetails = null;
        CancellationToken capturedCancellationToken = default;
        var adapter = new WebPushClientAdapter((subscription, payload, vapidDetails, cancellationToken) => {
            capturedSubscription = subscription;
            capturedPayload = payload;
            capturedVapidDetails = vapidDetails;
            capturedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        });
        var vapidDetails = new VapidDetails("mailto:test@example.com", "public", "private");
        var subscription = new PushSubscription("https://push.example.com/subscriptions/test", "p256dh", "auth");
        using var cancellationTokenSource = new CancellationTokenSource();

        await adapter.SendNotificationAsync(subscription, "payload", vapidDetails, cancellationTokenSource.Token);

        Assert.Same(subscription, capturedSubscription);
        Assert.Equal("payload", capturedPayload);
        Assert.Same(vapidDetails, capturedVapidDetails);
        Assert.Equal(cancellationTokenSource.Token, capturedCancellationToken);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenClientFails_PropagatesExceptionFromAwaitedDelegate() {
        var expected = new InvalidOperationException("push failed");
        var adapter = new WebPushClientAdapter((_, _, _, _) => Task.FromException(expected));
        var vapidDetails = new VapidDetails("mailto:test@example.com", "public", "private");
        var subscription = new PushSubscription("https://push.example.com/subscriptions/test", "p256dh", "auth");

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.SendNotificationAsync(subscription, "payload", vapidDetails, CancellationToken.None));

        Assert.Same(expected, ex);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenCancellationIsRequested_PropagatesCancellation() {
        var adapter = new WebPushClientAdapter();
        VapidDetails keys = VapidHelper.GenerateVapidKeys();
        var vapidDetails = new VapidDetails("mailto:test@example.com", keys.PublicKey, keys.PrivateKey);
        var subscription = new PushSubscription("https://push.example.com/subscriptions/test", p256dh: null, auth: null);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => adapter.SendNotificationAsync(
            subscription,
            payload: string.Empty,
            vapidDetails,
            cancellationTokenSource.Token));
    }
}
