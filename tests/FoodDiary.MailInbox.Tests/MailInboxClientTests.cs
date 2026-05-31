using System.Net;
using System.Net.Http.Json;
using FoodDiary.MailInbox.Client;
using FoodDiary.MailInbox.Client.Models;
using FoodDiary.MailInbox.Client.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Tests;

public sealed class MailInboxClientTests {
    [Fact]
    public async Task GetMessagesAsync_SendsApiKeyHeader() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(Array.Empty<InboundMailMessageSummaryResponse>())
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test")
        };
        var client = new MailInboxClient(httpClient, Options.Create(new MailInboxClientOptions {
            BaseUrl = "https://inbox.example.test",
            ApiKey = "secret"
        }));

        var messages = await client.GetMessagesAsync(10, CancellationToken.None);

        Assert.Empty(messages);
        Assert.Equal(HttpMethod.Get, handler.Request?.Method);
        Assert.Equal("https://inbox.example.test/api/mail-inbox/messages?limit=10", handler.Request?.RequestUri?.ToString());
        Assert.Equal("secret", handler.Request?.Headers.GetValues("X-MailInbox-Api-Key").Single());
    }

    [Fact]
    public async Task GetMessageAsync_WhenBaseAddressIsMissing_Throws() {
        using var httpClient = new HttpClient(new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new MailInboxClient(httpClient, Options.Create(new MailInboxClientOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetMessageAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Request = request;
            return Task.FromResult(response);
        }
    }
}
