using System.Net;
using System.Text;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class PaddleNotificationRecoveryServiceTests {
    [Fact]
    public void HasValidConfiguration_ProductionWithoutNotificationSettingId_ReturnsFalse() {
        var options = new PaddleOptions {
            Environment = PaddleOptions.ProductionEnvironment,
            ApiKey = "paddle-api-key",
            ApiBaseUrl = "https://api.paddle.com",
            ClientSideToken = "live_client-token",
            WebhookSecretKey = "pdl_ntfset_secret",
            NotificationSettingId = string.Empty,
            PremiumMonthlyPriceId = "pri_month",
            PremiumYearlyPriceId = "pri_year",
            CheckoutUrl = "https://example.com/premium",
        };

        Assert.False(PaddleOptions.HasValidConfiguration(options));
    }

    [Fact]
    public async Task ReplayFailedAsync_ReplaysOnlyOriginalNotificationsNotPreviouslyReplayed() {
        var handler = new QueueHandler(
            JsonResponse("""
                {
                  "data": [
                    { "id": "ntf_failed", "origin": "event", "replayed_at": null },
                    { "id": "ntf_already_replayed", "origin": "event", "replayed_at": "2026-08-08T08:00:00Z" },
                    { "id": "ntf_replay", "origin": "replay", "replayed_at": null }
                  ],
                  "meta": { "pagination": { "next": null } }
                }
                """),
            new HttpResponseMessage(HttpStatusCode.Accepted) { Content = JsonContent("{}") });
        var service = new PaddleNotificationRecoveryService(
            new HttpClient(handler),
            MsOptions.Create(ValidOptions()));

        PaddleNotificationRecoveryResult result = await service.ReplayFailedAsync(CancellationToken.None);

        Assert.Equal(3, result.Inspected);
        Assert.Equal(1, result.Replayed);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.EndsWith("/notifications/ntf_failed/replay", handler.Requests[1].RequestUri?.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal("Bearer", handler.Requests[0].Headers.Authorization?.Scheme);
        Assert.Equal("1", Assert.Single(handler.Requests[0].Headers.GetValues("Paddle-Version")));
    }

    [Fact]
    public async Task ReplayFailedAsync_WhenConfigurationIsIncomplete_DoesNotCallPaddle() {
        var handler = new QueueHandler(JsonResponse("{}"));
        var service = new PaddleNotificationRecoveryService(
            new HttpClient(handler),
            MsOptions.Create(new PaddleOptions()));

        PaddleNotificationRecoveryResult result = await service.ReplayFailedAsync(CancellationToken.None);

        Assert.Equal(new PaddleNotificationRecoveryResult(0, 0), result);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("https://sandbox-api.paddle.com/notifications?page=2")]
    [InlineData("notifications?page=2")]
    public async Task ReplayFailedAsync_WithNextPage_FollowsAbsoluteAndRelativeLinks(string next) {
        var handler = new QueueHandler(
            JsonResponse($"{{\"data\":[],\"meta\":{{\"pagination\":{{\"next\":\"{next}\"}}}}}}"),
            JsonResponse("""{"data":[],"meta":{"pagination":{"next":null}}}"""));
        var service = new PaddleNotificationRecoveryService(new HttpClient(handler), MsOptions.Create(ValidOptions()));

        PaddleNotificationRecoveryResult result = await service.ReplayFailedAsync(CancellationToken.None);

        Assert.Equal(new PaddleNotificationRecoveryResult(0, 0), result);
        Assert.Equal(2, handler.Requests.Count);
        Assert.EndsWith("/notifications?page=2", handler.Requests[1].RequestUri?.AbsoluteUri, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("live_client-token", "paddle-api-key", true)]
    [InlineData("test_client-token", "paddle-api-key", false)]
    [InlineData("live_client-token", "paddle-sdbx-key", false)]
    public void HasMatchingEnvironment_ProductionValidatesTokenAndApiKey(
        string clientSideToken,
        string apiKey,
        bool expected) {
        var options = new PaddleOptions {
            Environment = PaddleOptions.ProductionEnvironment,
            ApiBaseUrl = "https://api.paddle.com/",
            ClientSideToken = clientSideToken,
            ApiKey = apiKey,
        };

        Assert.Equal(expected, PaddleOptions.HasMatchingEnvironment(options));
    }

    [Fact]
    public void HasMatchingEnvironment_WhenEnvironmentIsUnknown_ReturnsFalse() {
        var options = new PaddleOptions {
            Environment = "Development",
            ApiBaseUrl = "https://api.paddle.com",
            ClientSideToken = "live_client-token",
            ApiKey = "paddle-api-key",
        };

        Assert.False(PaddleOptions.HasMatchingEnvironment(options));
    }

    private static PaddleOptions ValidOptions() => new() {
        Environment = PaddleOptions.SandboxEnvironment,
        ApiKey = "paddle-api-key",
        ApiBaseUrl = "https://sandbox-api.paddle.com",
        ClientSideToken = "test_client-token",
        WebhookSecretKey = "pdl_ntfset_secret",
        NotificationSettingId = "ntfset_01abcdefghijklmnopqrstuvwx",
        PremiumMonthlyPriceId = "pri_month",
        PremiumYearlyPriceId = "pri_year",
        CheckoutUrl = "https://example.com/premium",
    };

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = JsonContent(json) };

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    [ExcludeFromCodeCoverage]
    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            Requests.Add(request);
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
