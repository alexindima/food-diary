using System.Net;
using System.Net.Http.Json;
using FoodDiary.MailRelay.Client.Extensions;
using FoodDiary.MailRelay.Client.Journal;
using FoodDiary.MailRelay.Client.Models;
using FoodDiary.MailRelay.Client.Options;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.MailRelay.Client.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayJournalClientTests {
    [Fact]
    public async Task GetPage_EncodesFiltersAndReturnsDeliveryMetadata() {
        var entry = new OutgoingEmailJournalEntry(Guid.NewGuid(), "sent", "welcome", "from@example.com", ["to@example.com"], "subject", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(1), 1, 3, "trace", "body", ContentHidden: false, "reply@example.com", "message-id");
        var page = new OutgoingEmailJournalPage([entry], 31);
        using var handler = new RecordingHandler(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(page) });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://relay.example.com") };
        var client = new MailRelayJournalClient(http, Microsoft.Extensions.Options.Options.Create(new MailRelayClientOptions { ApiKey = "test-key" }));
        OutgoingEmailJournalPage actual = await client.GetPageAsync(2, 10, "welcome", "sent", "to+tag@example.com", CancellationToken.None, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), entry.Id, "trace & id");
        Assert.Equivalent(page, actual, strict: true);
        Assert.Equal("test-key", handler.Key);
        Assert.NotNull(handler.Uri);
        Assert.Equal("/api/email/messages", handler.Uri.AbsolutePath);
        Assert.Contains("recipient=to%2Btag%40example.com", handler.Uri.Query, StringComparison.Ordinal);
        Assert.Contains("correlationId=trace%20%26%20id", handler.Uri.Query, StringComparison.Ordinal);
        Assert.Contains("page=2&limit=10", handler.Uri.Query, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "{}")]
    [InlineData(HttpStatusCode.OK, "null")]
    public async Task GetPage_RejectsHttpFailureAndNullPayload(HttpStatusCode status, string json) {
        using var handler = new RecordingHandler(() => new HttpResponseMessage(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://relay.example.com") };
        var client = new MailRelayJournalClient(http, Microsoft.Extensions.Options.Options.Create(new MailRelayClientOptions { ApiKey = "test-key" }));
        if (status == HttpStatusCode.OK) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetPageAsync(1, 50, purpose: null, status: null, recipient: null, CancellationToken.None));
        } else {
            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetPageAsync(1, 50, purpose: null, status: null, recipient: null, CancellationToken.None));
        }
    }

    [Fact]
    public void Registration_ResolvesJournalClient() {
        var services = new ServiceCollection();
        services.AddMailRelayClient(options => { options.BaseUrl = "https://relay.example.com"; options.ApiKey = "test-key"; });
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<MailRelayJournalClient>(provider.GetRequiredService<IMailRelayJournalClient>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHandler(Func<HttpResponseMessage> response) : HttpMessageHandler {
        public Uri? Uri { get; private set; }
        public string? Key { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Uri = request.RequestUri;
            Key = request.Headers.GetValues("X-Relay-Api-Key").Single();
            return Task.FromResult(response());
        }
    }
}
