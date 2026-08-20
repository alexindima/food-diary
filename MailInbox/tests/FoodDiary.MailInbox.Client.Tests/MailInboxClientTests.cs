using System.Net;
using System.Net.Http.Json;
using FoodDiary.MailInbox.Client.Extensions;
using FoodDiary.MailInbox.Client.Models;
using FoodDiary.MailInbox.Client.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Client.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxClientTests {
    [Fact]
    public async Task GetMessagesAsync_WhenPayloadContainsAuthenticationProvenance_ReturnsIt() {
        var expected = new InboundMailMessageSummaryResponse(
            Guid.NewGuid(),
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            "general",
            "Received",
            ReadAtUtc: null,
            DateTimeOffset.UtcNow,
            EnvelopeFromAddress: "bounce@relay.example",
            IsTrustedRelay: true,
            FromAddressIsVerified: false);
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(new[] { expected }),
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions()));

        InboundMailMessageSummaryResponse message = Assert.Single(
            await client.GetMessagesAsync(limit: null, CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal("bounce@relay.example", message.EnvelopeFromAddress),
            () => Assert.True(message.IsTrustedRelay),
            () => Assert.False(message.FromAddressIsVerified));
    }

    [Fact]
    public async Task GetMessagesAsync_SendsApiKeyHeader() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(Array.Empty<InboundMailMessageSummaryResponse>()),
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions {
            BaseUrl = "https://inbox.example.test",
            MetadataApiKey = "metadata-secret",
        }));

        IReadOnlyList<InboundMailMessageSummaryResponse> messages = await client.GetMessagesAsync(10, CancellationToken.None);

        Assert.Empty(messages);
        Assert.Multiple(
            () => Assert.Equal(HttpMethod.Get, handler.Request?.Method),
            () => Assert.Equal("https://inbox.example.test/api/mail-inbox/messages?limit=10", handler.Request?.RequestUri?.ToString()),
            () => Assert.Equal("metadata-secret", handler.Request?.Headers.GetValues("X-MailInbox-Api-Key").Single()));
    }

    [Fact]
    public async Task GetMessageAsync_WhenBaseAddressIsMissing_Throws() {
        using var httpClient = new HttpClient(new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetMessageAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetMessagesAsync_WhenResponseIsInvalidJson_ThrowsInvalidOperationException() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{"),
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMessagesAsync(limit: null, CancellationToken.None));

        Assert.Contains("invalid message list", exception.Message, StringComparison.Ordinal);
        Assert.Equal("https://inbox.example.test/api/mail-inbox/messages", handler.Request?.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetMessagesAsync_WhenPayloadIsNull_ThrowsInvalidOperationException() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("null"),
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMessagesAsync(limit: null, CancellationToken.None));

        Assert.Contains("empty message list", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMessageAsync_WhenNotFound_ReturnsNull() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions {
            ContentApiKey = "content-secret",
        }));

        InboundMailMessageDetailsResponse? message = await client.GetMessageAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None);

        Assert.Multiple(
            () => Assert.Null(message),
            () => Assert.Equal("content-secret", handler.Request?.Headers.GetValues("X-MailInbox-Api-Key").Single()));
    }

    [Fact]
    public async Task GetMessageAsync_WhenResponseIsInvalidJson_ThrowsInvalidOperationException() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{"),
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMessageAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("invalid message details", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMessageAsync_WhenPayloadIsValid_ReturnsDetails() {
        var id = Guid.NewGuid();
        var dmarcReport = new DmarcReportResponse(
            "google.com",
            "report-1",
            "fooddiary.club",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            [new DmarcReportRecordResponse(
                SourceIp: "192.0.2.1",
                Count: 4,
                Disposition: "none",
                Dkim: "pass",
                Spf: "pass",
                HeaderFrom: "fooddiary.club",
                EnvelopeFrom: null,
                DkimDomain: "fooddiary.club",
                DkimResult: "pass",
                SpfDomain: "fooddiary.club",
                SpfResult: "pass")]);
        var expected = new InboundMailMessageDetailsResponse(
            id,
            "message-id",
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            "text",
            "<p>text</p>",
            "raw",
            "general",
            "Received",
            ReadAtUtc: null,
            DateTimeOffset.UtcNow,
            ContentPurgedAtUtc: null,
            dmarcReport,
            EnvelopeFromAddress: "bounce@relay.example",
            IsTrustedRelay: true,
            FromAddressIsVerified: false,
            DmarcReportIsVerified: false);
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(expected),
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions()));

        InboundMailMessageDetailsResponse? message = await client.GetMessageAsync(id, CancellationToken.None);

        Assert.NotNull(message);
        Assert.Multiple(
            () => Assert.Equal(expected.Id, message.Id),
            () => Assert.Equal(expected.MessageId, message.MessageId),
            () => Assert.Equal(expected.FromAddress, message.FromAddress),
            () => Assert.Equal(expected.ToRecipients, message.ToRecipients),
            () => Assert.Equal(expected.RawMime, message.RawMime),
            () => Assert.Equal("report-1", message.DmarcReport?.ReportId),
            () => Assert.Equal(4, Assert.Single(message.DmarcReport!.Records).Count),
            () => Assert.Equal("bounce@relay.example", message.EnvelopeFromAddress),
            () => Assert.True(message.IsTrustedRelay),
            () => Assert.False(message.FromAddressIsVerified),
            () => Assert.False(message.DmarcReportIsVerified));
    }

    [Fact]
    public async Task GetMessageAsync_WhenPayloadIsNull_ThrowsInvalidOperationException() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("null"),
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetMessageAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("empty message details", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MarkMessageReadAsync_WhenSuccessful_SendsExpectedRequestAndReturnsTrue() {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions {
            StateApiKey = "state-secret",
        }));

        bool result = await client.MarkMessageReadAsync(id, CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result),
            () => Assert.Equal(HttpMethod.Post, handler.Request?.Method),
            () => Assert.Equal("https://inbox.example.test/api/mail-inbox/messages/11111111-1111-1111-1111-111111111111/read", handler.Request?.RequestUri?.ToString()),
            () => Assert.Equal("state-secret", handler.Request?.Headers.GetValues("X-MailInbox-Api-Key").Single()));
    }

    [Fact]
    public async Task MarkMessageReadAsync_WhenNotFound_ReturnsFalse() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://inbox.example.test"),
        };
        var client = new MailInboxClient(httpClient, Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions()));

        bool result = await client.MarkMessageReadAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
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
            "general",
            "received",
            ReadAtUtc: null,
            receivedAtUtc);

        Assert.Multiple(
            () => Assert.Equal(id, response.Id),
            () => Assert.Equal("sender@example.com", response.FromAddress),
            () => Assert.Equal(["admin@fooddiary.club"], response.ToRecipients),
            () => Assert.Equal("Hello", response.Subject),
            () => Assert.Equal("received", response.Status),
            () => Assert.Equal("general", response.Category),
            () => Assert.Null(response.ReadAtUtc),
            () => Assert.Equal(receivedAtUtc, response.ReceivedAtUtc));
    }

    [Theory]
    [InlineData("https://inbox.example.test", false, true)]
    [InlineData("http://localhost:5098", true, true)]
    [InlineData("http://127.0.0.1:5098", true, true)]
    [InlineData("http://inbox.example.test", true, false)]
    [InlineData("http://localhost:5098", false, false)]
    [InlineData("ftp://inbox.example.test", false, false)]
    [InlineData("https://user:password@inbox.example.test", false, false)]
    [InlineData("https://inbox.example.test?secret=value", false, false)]
    [InlineData("https://inbox.example.test#fragment", false, false)]
    [InlineData("not-a-url", false, false)]
    [InlineData("", false, false)]
    public void MailInboxClientOptions_HasValidBaseUrl_ReturnsExpectedResult(
        string baseUrl,
        bool allowInsecureLoopback,
        bool expected) {
        var options = new MailInboxClientOptions {
            BaseUrl = baseUrl,
            AllowInsecureLoopback = allowInsecureLoopback,
        };

        Assert.Equal(expected, MailInboxClientOptions.HasValidBaseUrl(options));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("too-short", false)]
    [InlineData("0123456789abcdef0123456789abcdea", false)]
    [InlineData("fedcba9876543210fedcba987654321d", true)]
    public void MailInboxClientOptions_HasValidApiKey_ReturnsExpectedResult(string apiKey, bool expected) {
        var options = new MailInboxClientOptions {
            MetadataApiKey = apiKey,
            ContentApiKey = "fedcba9876543210fedcba987654321b",
            StateApiKey = "fedcba9876543210fedcba987654321c",
        };

        Assert.Equal(expected, MailInboxClientOptions.HasValidApiKey(options));
    }

    [Fact]
    public void AddMailInboxClient_ConfiguresHttpClientAndOptions() {
        var services = new ServiceCollection();
        services.AddMailInboxClient(options => {
            options.BaseUrl = "https://inbox.example.test";
            options.MetadataApiKey = "fedcba9876543210fedcba987654321a";
            options.ContentApiKey = "fedcba9876543210fedcba987654321b";
            options.StateApiKey = "fedcba9876543210fedcba987654321c";
            options.Timeout = TimeSpan.FromSeconds(3);
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        MailInboxClientOptions options = provider.GetRequiredService<IOptions<MailInboxClientOptions>>().Value;
        IHttpClientFactory clientFactory = provider.GetRequiredService<IHttpClientFactory>();
        using HttpClient httpClient = clientFactory.CreateClient(nameof(IMailInboxClient));

        Assert.Multiple(
            () => Assert.Equal("https://inbox.example.test", options.BaseUrl),
            () => Assert.Equal(TimeSpan.FromSeconds(3), options.Timeout),
            () => Assert.Equal("https://inbox.example.test/", httpClient.BaseAddress?.ToString()),
            () => Assert.Equal(TimeSpan.FromSeconds(3), httpClient.Timeout));
    }

    [Fact]
    public void AddMailInboxClient_WhenApiKeyIsInvalid_RejectsOptions() {
        var services = new ServiceCollection();
        services.AddMailInboxClient(options => {
            options.BaseUrl = "https://inbox.example.test";
            options.MetadataApiKey = "too-short";
            options.ContentApiKey = "fedcba9876543210fedcba987654321b";
            options.StateApiKey = "fedcba9876543210fedcba987654321c";
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<MailInboxClientOptions>>().Value);
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
