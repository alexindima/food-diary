using System.Net;
using System.Net.Http.Json;
using System.Text;
using FoodDiary.Telegram.Bot.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Model;
using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Options;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotGeneratedClientTests {
    [Fact]
    public async Task RecognitionAsync_PreservesDecimalPrecisionAndNullableErrorFields() {
        const decimal expected = 1.234567890123456789m;
        using var handler = new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{\"id\":\"11111111-1111-1111-1111-111111111111\",\"imageAssetId\":\"22222222-2222-2222-2222-222222222222\",\"status\":\"Succeeded\",\"errorCode\":null,\"nutritionErrorCode\":null,\"nutrition\":{\"calories\":0,\"protein\":1.234567890123456789,\"fat\":0,\"carbs\":0}}", Encoding.UTF8, "application/json"),
        }));
        using var http = new HttpClient(handler);
        BotRecognitionJob result = await Client(http).GetRecognitionAsync("token", Guid.NewGuid(), CancellationToken.None);
        Assert.NotNull(result.Nutrition);
        Assert.Multiple(() => {
            Assert.Equal(expected, result.Nutrition.Protein);
            Assert.Equal(0m, result.Nutrition.Calories);
            Assert.Null(result.ErrorCode);
            Assert.Null(result.NutritionErrorCode);
        });
    }

    [Fact]
    public async Task WaterAsync_PreservesUtcSubsecondTimestampAndReceiptIdentity() {
        var operationId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        DateTime timestamp = new DateTime(2026, 10, 7, 9, 30, 0, DateTimeKind.Utc).AddTicks(1234567);
        using var handler = new Handler(async request => {
            using var body = System.Text.Json.JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(timestamp, body.RootElement.GetProperty("timestampUtc").GetDateTime());
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { OperationId = operationId, EntryId = entryId, TimestampUtc = timestamp, AmountMl = 250 }) };
        });
        using var http = new HttpClient(handler);
        BotHydrationReceipt result = await Client(http).SaveWaterAsync("token", new BotOperationId(operationId), timestamp, 250, CancellationToken.None);
        Assert.Multiple(() => {
            Assert.Equal(timestamp, result.TimestampUtc);
            Assert.Equal(DateTimeKind.Utc, result.TimestampUtc.Kind);
            Assert.Equal(operationId, result.OperationId);
            Assert.Equal(entryId, result.EntryId);
        });
    }

    [Fact]
    public async Task GeneratedCalls_DoNotStoreCredentialsOnTheSharedHttpClient() {
        using var handler = new Handler(request => {
            if (request.RequestUri!.AbsolutePath.EndsWith("/bot/auth", StringComparison.Ordinal)) {
                Assert.Null(request.Headers.Authorization);
                Assert.Equal("secret", Assert.Single(request.Headers.GetValues("X-Telegram-Bot-Secret")));
            } else {
                Assert.Equal("user-token", request.Headers.Authorization?.Parameter);
                Assert.False(request.Headers.Contains("X-Telegram-Bot-Secret"));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var http = new HttpClient(handler);
        var transport = new BotApiTransport(http, new Uri("https://diary.example"));
        using HttpResponseMessage auth = await new BotAuthApi(transport).AuthenticateAsync("1", new TelegramBotAuthHttpRequest { TelegramUserId = 123 }, new BotApiRequestContext(ApiSecret: "secret"), CancellationToken.None);
        using HttpResponseMessage recognition = await new BotRecognitionApi(transport).GetRecognitionAsync(Guid.NewGuid(), "1", new BotApiRequestContext("user-token"), CancellationToken.None);
        Assert.Empty(http.DefaultRequestHeaders);
    }

    [Fact]
    public async Task GeneratedCall_PropagatesCancellationToTheHttpHandler() {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new CancellationHandler(entered);
        using var http = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        Task<BotRecognitionJob> pending = Client(http).GetRecognitionAsync("token", Guid.NewGuid(), cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    private static BotDiaryClient Client(HttpClient http) => new(http, Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://diary.example", ApiSecret = "secret" }));

    [ExcludeFromCodeCoverage]
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }

    [ExcludeFromCodeCoverage]
    private sealed class CancellationHandler(TaskCompletionSource entered) : HttpMessageHandler {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Cancellation was not propagated.");
        }
    }
}
