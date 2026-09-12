using System.Net;
using System.Net.Http.Json;
using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Options;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotRecognitionAccessTests {
    [Theory]
    [InlineData("Ai.ConsentRequired", HttpStatusCode.Forbidden)]
    [InlineData("Ai.QuotaExceeded", HttpStatusCode.TooManyRequests)]
    public async Task StartRecognitionAsync_PreservesKnownAccessReasonAsync(string code, HttpStatusCode status) {
        using var handler = new AccessHandler(code, status);
        using var http = new HttpClient(handler);
        var client = new BotDiaryClient(http, Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://diary.example" }));
        BotRecognitionAccessException error = await Assert.ThrowsAsync<BotRecognitionAccessException>(() =>
            client.StartRecognitionAsync("token", Guid.NewGuid(), Guid.NewGuid(), caption: null, CancellationToken.None));
        Assert.Equal(code, error.Code);
    }

    [Fact]
    public async Task StartRecognitionAsync_UnknownRateLimitRemainsRetryableAsync() {
        using var handler = new AccessHandler("RateLimit", HttpStatusCode.TooManyRequests);
        using var http = new HttpClient(handler);
        var client = new BotDiaryClient(http, Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://diary.example" }));
        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.StartRecognitionAsync("token", Guid.NewGuid(), Guid.NewGuid(), caption: null, CancellationToken.None));
        Assert.Equal(HttpStatusCode.TooManyRequests, error.StatusCode);
    }

    [ExcludeFromCodeCoverage]
    private sealed class AccessHandler(string code, HttpStatusCode status) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(new { error = code }) });
    }
}
