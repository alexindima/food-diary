using System.Net;
using FoodDiary.Modules.Notifications.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebPush;

namespace FoodDiary.Modules.Notifications.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class WebPushResponseLimitTests {
    private const int ResponseLimit = 64 * 1024;

    [Theory]
    [InlineData(HttpStatusCode.Created, true)]
    [InlineData(HttpStatusCode.Created, false)]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    public async Task SendNotificationAsync_WhenResponseExceedsLimit_RejectsBeforeBufferingEntireBody(
        HttpStatusCode status, bool declaredLength) {
        using var content = new GeneratedContent(ResponseLimit * 4, declaredLength);
        await using ServiceProvider provider = CreateProvider(status, content);
        IWebPushClientAdapter adapter = provider.GetRequiredService<IWebPushClientAdapter>();

        await Assert.ThrowsAsync<HttpRequestException>(() => SendAsync(adapter));

        Assert.InRange(content.BytesWritten, 0, ResponseLimit + 1024);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendNotificationAsync_WhenSuccessResponseFitsLimit_Completes(bool declaredLength) {
        using var content = new GeneratedContent(ResponseLimit, declaredLength);
        await using ServiceProvider provider = CreateProvider(HttpStatusCode.Created, content);

        await SendAsync(provider.GetRequiredService<IWebPushClientAdapter>());

        Assert.Equal(ResponseLimit, content.BytesWritten);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendNotificationAsync_WhenErrorResponseFitsLimit_PreservesProviderFailure(bool declaredLength) {
        using var content = new GeneratedContent(32, declaredLength);
        await using ServiceProvider provider = CreateProvider(HttpStatusCode.BadRequest, content);

        WebPushException error = await Assert.ThrowsAsync<WebPushException>(() =>
            SendAsync(provider.GetRequiredService<IWebPushClientAdapter>()));

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode),
            () => Assert.Equal(32, content.BytesWritten));
    }

    private static ServiceProvider CreateProvider(HttpStatusCode status, HttpContent content) {
        var services = new ServiceCollection();
        services.AddNotificationsProvider(new ConfigurationBuilder().Build());
        services.AddHttpClient<IWebPushClientAdapter, WebPushClientAdapter>()
            .ConfigurePrimaryHttpMessageHandler(() => new ResponseHandler(status, content));
        return services.BuildServiceProvider();
    }

    private static Task SendAsync(IWebPushClientAdapter adapter) {
        VapidDetails keys = VapidHelper.GenerateVapidKeys();
        return adapter.SendNotificationAsync(
            new PushSubscription("https://push.example.com/subscription", p256dh: null, auth: null),
            string.Empty,
            new VapidDetails("mailto:test@example.com", keys.PublicKey, keys.PrivateKey),
            CancellationToken.None);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ResponseHandler(HttpStatusCode status, HttpContent content) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = content });
    }

    [ExcludeFromCodeCoverage]
    private sealed class GeneratedContent(int bodyLength, bool declaredLength) : HttpContent {
        public int BytesWritten { get; private set; }

        protected override bool TryComputeLength(out long length) {
            length = bodyLength;
            return declaredLength;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context) {
            byte[] chunk = new byte[1024];
            while (BytesWritten < bodyLength) {
                int count = Math.Min(chunk.Length, bodyLength - BytesWritten);
                await stream.WriteAsync(chunk.AsMemory(0, count));
                BytesWritten += count;
            }
        }
    }
}
