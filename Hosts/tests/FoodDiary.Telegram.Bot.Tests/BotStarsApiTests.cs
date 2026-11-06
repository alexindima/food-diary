using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Telegram.Bot.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Model;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotStarsApiTests {
    [Fact]
    public async Task ProcessUpdateAsync_PostsFinancialEnvelopeWithBotAuthenticationAndReturnsTransportResponse() {
        using var handler = new StarsHandler();
        using var http = new HttpClient(handler);
        var api = new BotStarsApi(new BotApiTransport(http, new Uri("https://diary.example")));
        var update = new TelegramStarsUpdateHttpRequest {
            UpdateId = 42,
            TelegramUserId = 123,
            Kind = "pre_checkout",
            InvoicePayload = "owned-invoice",
            Currency = "XTR",
            TotalAmount = 500,
        };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using HttpResponseMessage response = await api.ProcessUpdateAsync("1", update,
            new BotApiRequestContext(ApiSecret: "synthetic-secret"), cancellation.Token);
        TelegramStarsUpdateHttpResponse? result = await response.Content.ReadFromJsonAsync<TelegramStarsUpdateHttpResponse>(cancellation.Token);

        Assert.NotNull(result);
        Assert.Multiple(() => {
            Assert.False(result.CheckoutApproved);
            Assert.Equal("Invoice is unavailable", result.Error);
        });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(http.DefaultRequestHeaders);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StarsHandler : HttpMessageHandler {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            JsonElement root = payload.RootElement;
            Assert.Multiple(() => {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/v1/billing/telegram-stars/updates", request.RequestUri!.AbsolutePath);
                Assert.Equal("synthetic-secret", Assert.Single(request.Headers.GetValues("X-Telegram-Bot-Secret")));
                Assert.Null(request.Headers.Authorization);
                Assert.True(cancellationToken.CanBeCanceled);
                Assert.Equal(42, root.GetProperty("updateId").GetInt64());
                Assert.Equal(123, root.GetProperty("telegramUserId").GetInt64());
                Assert.Equal("pre_checkout", root.GetProperty("kind").GetString());
                Assert.Equal("owned-invoice", root.GetProperty("invoicePayload").GetString());
                Assert.Equal("XTR", root.GetProperty("currency").GetString());
                Assert.Equal(500, root.GetProperty("totalAmount").GetInt32());
            });
            return new HttpResponseMessage(HttpStatusCode.Accepted) {
                Content = JsonContent.Create(new { checkoutApproved = false, error = "Invoice is unavailable" }),
            };
        }
    }
}
