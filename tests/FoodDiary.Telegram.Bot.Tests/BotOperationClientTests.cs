using System.Net;
using System.Net.Http.Json;
using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Options;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotOperationClientTests {
    [Fact]
    public async Task RegisterAsync_SendsStablePayloadAndBotSecretToConfiguredApi() {
        var id = Guid.NewGuid();
        using var handler = new Handler(async request => {
            Assert.Equal("https://api.example.com/api/v1/auth/telegram/bot/operations", request.RequestUri!.AbsoluteUri);
            Assert.Equal("test-operation-secret", Assert.Single(request.Headers.GetValues("X-Telegram-Bot-Secret")));
            string body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("stable-payload", body, StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { OperationId = id }) };
        });
        using var http = new HttpClient(handler);
        Guid actual = await CreateClient(http).RegisterAsync(10, 123, "stable-payload", CancellationToken.None);
        Assert.Equal(id, actual);
    }

    [Fact]
    public async Task AcquireAsync_ConflictDoesNotPretendWorkWasGranted() {
        using var handler = new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)));
        using var http = new HttpClient(handler);
        Assert.Null(await CreateClient(http).AcquireAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task AcquireAsync_RejectsLeaseForDifferentOperation() {
        var lease = new BotOperationLease(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "payload", Checkpoint: null, DateTime.UtcNow.AddMinutes(2));
        using var handler = new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(lease) }));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => CreateClient(http).AcquireAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListReadyAsync_RejectsOversizedApiResponse() {
        using var handler = new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string(' ', 131073)) }));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateClient(http).ListReadyAsync(CancellationToken.None));
    }

    private static BotOperationClient CreateClient(HttpClient http) => new(http, Options.Create(new TelegramBotOptions {
        ApiBaseUrl = "https://api.example.com",
        ApiSecret = "test-operation-secret",
    }));

    [ExcludeFromCodeCoverage]
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
