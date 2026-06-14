using FoodDiary.Integrations.Services;
using WebPush;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class WebPushClientAdapterTests {
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
