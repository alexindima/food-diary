using System.Net;
using System.Text.Json;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

public partial class BillingGatewayTests {
    [Theory]
    [InlineData(false, "/bot123:synthetic/sendMessage")]
    [InlineData(true, "/bot123:synthetic/test/sendMessage")]
    public async Task TelegramConfirmation_UsesOwnedBotAndPlainTextEvenWhenCheckoutIsOff(bool test, string path) {
        using var http = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("{\"ok\":true,\"result\":{\"message_id\":123}}"),
        });
        using var client = new HttpClient(http);
        var sender = new TelegramBillingMessageSender(client, Options.Create(new TelegramStarsOptions {
            BotToken = "123:synthetic",
            UseTestEnvironment = test,
            CheckoutEnabled = false,
        }));
        const string text = "Автопродление отключено.\nPremium действует до 08.12.2026.";

        Assert.Equal(TelegramMessageDeliveryResult.Sent, await sender.SendAsync(456, text, CancellationToken.None));

        Assert.Equal("https", http.LastRequest!.RequestUri!.Scheme);
        Assert.Equal("api.telegram.org", http.LastRequest.RequestUri.Host);
        Assert.Equal(path, http.LastRequest.RequestUri.AbsolutePath);
        using var payload = JsonDocument.Parse(http.LastRequestBody);
        Assert.Equal(456, payload.RootElement.GetProperty("chat_id").GetInt64());
        Assert.Equal(text, payload.RootElement.GetProperty("text").GetString());
        Assert.False(payload.RootElement.TryGetProperty("parse_mode", out _));
        Assert.False(payload.RootElement.TryGetProperty("allow_paid_broadcast", out _));
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, TelegramMessageDeliveryResult.RecipientUnavailable)]
    [InlineData(HttpStatusCode.Unauthorized, TelegramMessageDeliveryResult.Retry)]
    [InlineData(HttpStatusCode.BadRequest, TelegramMessageDeliveryResult.Retry)]
    [InlineData(HttpStatusCode.TooManyRequests, TelegramMessageDeliveryResult.Retry)]
    [InlineData(HttpStatusCode.InternalServerError, TelegramMessageDeliveryResult.Retry)]
    public async Task TelegramConfirmation_ClassifiesForbiddenAndRetryableProviderFailures(HttpStatusCode status, TelegramMessageDeliveryResult expected) {
        using var http = new RecordingHttpMessageHandler(new HttpResponseMessage(status));
        using var client = new HttpClient(http);
        var sender = new TelegramBillingMessageSender(client, Options.Create(new TelegramStarsOptions { BotToken = "123:synthetic" }));
        Assert.Equal(expected, await sender.SendAsync(456, "confirmation", CancellationToken.None));
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("{\"ok\":false}")]
    [InlineData("{\"ok\":true,\"result\":true}")]
    [InlineData("{\"ok\":true,\"result\":{\"message_id\":\"invalid\"}}")]
    public async Task TelegramConfirmation_InvalidAcknowledgementRetainsRetry(string body) {
        using var http = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(body) });
        using var client = new HttpClient(http);
        var sender = new TelegramBillingMessageSender(client, Options.Create(new TelegramStarsOptions { BotToken = "123:synthetic" }));
        Assert.Equal(TelegramMessageDeliveryResult.Retry, await sender.SendAsync(456, "confirmation", CancellationToken.None));
    }

    [Fact]
    public async Task TelegramConfirmation_UnconfiguredBotDoesNotMakeHttpCall() {
        using var http = new RecordingHttpMessageHandler();
        using var client = new HttpClient(http);
        var sender = new TelegramBillingMessageSender(client, Options.Create(new TelegramStarsOptions()));
        Assert.Equal(TelegramMessageDeliveryResult.Retry, await sender.SendAsync(456, "confirmation", CancellationToken.None));
        Assert.Empty(http.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TelegramConfirmation_TransportFailureDoesNotExposeToken(bool timeout) {
        Exception failure = timeout ? new TaskCanceledException("https://api.telegram.org/bot123:synthetic/sendMessage")
            : new HttpRequestException("https://api.telegram.org/bot123:synthetic/sendMessage");
        using var http = new ScriptedHttpMessageHandler(failure);
        using var client = new HttpClient(http);
        var sender = new TelegramBillingMessageSender(client, Options.Create(new TelegramStarsOptions { BotToken = "123:synthetic" }));
        Assert.Equal(TelegramMessageDeliveryResult.Retry, await sender.SendAsync(456, "confirmation", CancellationToken.None));
    }

    [Fact]
    public async Task TelegramConfirmation_CallerCancellationPropagates() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var http = new ScriptedHttpMessageHandler(new OperationCanceledException(cancellation.Token));
        using var client = new HttpClient(http);
        var sender = new TelegramBillingMessageSender(client, Options.Create(new TelegramStarsOptions { BotToken = "123:synthetic" }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendAsync(456, "confirmation", cancellation.Token));
    }
}
