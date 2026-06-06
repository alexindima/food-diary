using System.Net;
using System.Net.Http.Json;
using FoodDiary.MailInbox.Client;
using FoodDiary.MailInbox.Client.Extensions;
using FoodDiary.MailInbox.Client.Models;
using FoodDiary.MailInbox.Client.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Tests;

[ExcludeFromCodeCoverage]
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

        IReadOnlyList<InboundMailMessageSummaryResponse> messages = await client.GetMessagesAsync(10, CancellationToken.None);

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

    [Fact]
    public async Task GetMessagesAsync_WhenResponseIsInvalidJson_ThrowsInvalidOperationException() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{")
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test")
        };
        var client = new MailInboxClient(httpClient, Options.Create(new MailInboxClientOptions()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMessagesAsync(limit: null, CancellationToken.None));

        Assert.Contains("invalid message list", exception.Message, StringComparison.Ordinal);
        Assert.Equal("https://inbox.example.test/api/mail-inbox/messages", handler.Request?.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetMessagesAsync_WhenPayloadIsNull_ThrowsInvalidOperationException() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("null")
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test")
        };
        var client = new MailInboxClient(httpClient, Options.Create(new MailInboxClientOptions()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMessagesAsync(limit: null, CancellationToken.None));

        Assert.Contains("empty message list", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMessageAsync_WhenNotFound_ReturnsNull() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test")
        };
        var client = new MailInboxClient(httpClient, Options.Create(new MailInboxClientOptions()));

        InboundMailMessageDetailsResponse? message = await client.GetMessageAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None);

        Assert.Null(message);
    }

    [Fact]
    public async Task GetMessageAsync_WhenResponseIsInvalidJson_ThrowsInvalidOperationException() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{")
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test")
        };
        var client = new MailInboxClient(httpClient, Options.Create(new MailInboxClientOptions()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMessageAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("invalid message details", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMessageAsync_WhenPayloadIsValid_ReturnsDetails() {
        var id = Guid.NewGuid();
        var expected = new InboundMailMessageDetailsResponse(
            id,
            "message-id",
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            "text",
            "<p>text</p>",
            "raw",
            "Received",
            DateTimeOffset.UtcNow);
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(expected)
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test")
        };
        var client = new MailInboxClient(httpClient, Options.Create(new MailInboxClientOptions()));

        InboundMailMessageDetailsResponse? message = await client.GetMessageAsync(id, CancellationToken.None);

        Assert.NotNull(message);
        Assert.Equal(expected.Id, message.Id);
        Assert.Equal(expected.MessageId, message.MessageId);
        Assert.Equal(expected.FromAddress, message.FromAddress);
        Assert.Equal(expected.ToRecipients, message.ToRecipients);
        Assert.Equal(expected.RawMime, message.RawMime);
    }

    [Fact]
    public async Task GetMessageAsync_WhenPayloadIsNull_ThrowsInvalidOperationException() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("null")
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test")
        };
        var client = new MailInboxClient(httpClient, Options.Create(new MailInboxClientOptions()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMessageAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("empty message details", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InboundMailMessageSummaryResponse_ExposesConfiguredValues() {
        var id = Guid.NewGuid();
        DateTimeOffset receivedAtUtc = DateTimeOffset.UtcNow;

        var response = new InboundMailMessageSummaryResponse(
            id,
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            "received",
            receivedAtUtc);

        Assert.Equal(id, response.Id);
        Assert.Equal("sender@example.com", response.FromAddress);
        Assert.Equal(["admin@fooddiary.club"], response.ToRecipients);
        Assert.Equal("Hello", response.Subject);
        Assert.Equal("received", response.Status);
        Assert.Equal(receivedAtUtc, response.ReceivedAtUtc);
    }

    [Theory]
    [InlineData("https://inbox.example.test", true)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void MailInboxClientOptions_HasValidBaseUrl_ReturnsExpectedResult(string baseUrl, bool expected) {
        var options = new MailInboxClientOptions {
            BaseUrl = baseUrl
        };

        Assert.Equal(expected, MailInboxClientOptions.HasValidBaseUrl(options));
    }

    [Fact]
    public void AddMailInboxClient_ConfiguresHttpClientAndOptions() {
        var services = new ServiceCollection();
        services.AddMailInboxClient(options => {
            options.BaseUrl = "https://inbox.example.test";
            options.ApiKey = "secret";
            options.Timeout = TimeSpan.FromSeconds(3);
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        MailInboxClientOptions options = provider.GetRequiredService<IOptions<MailInboxClientOptions>>().Value;
        IHttpClientFactory clientFactory = provider.GetRequiredService<IHttpClientFactory>();
        using HttpClient httpClient = clientFactory.CreateClient(nameof(IMailInboxClient));

        Assert.Equal("https://inbox.example.test", options.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(3), options.Timeout);
        Assert.Equal("https://inbox.example.test/", httpClient.BaseAddress?.ToString());
        Assert.Equal(TimeSpan.FromSeconds(3), httpClient.Timeout);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Request = request;
            return Task.FromResult(response);
        }
    }
}
