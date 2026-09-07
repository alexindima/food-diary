using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FoodDiary.MailInbox.Client.Export;
using FoodDiary.MailInbox.Client.Models;
using FoodDiary.MailInbox.Client.Options;

namespace FoodDiary.MailInbox.Client.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxExportClientTests {
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task GetPageAsync_EncodesRecipientAndRequiresCompleteCursor(bool hasDate, bool hasId) {
        var entry = new MailInboxExportEntryResponse(Guid.NewGuid(), DateTimeOffset.UtcNow, ContentAvailable: true);
        using var handler = new ResponseHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(new[] { entry }),
        });
        using HttpClient http = CreateHttp(handler);
        MailInboxExportClient client = CreateClient(http);
        const string recipient = "bugs+export@example.test";

        IReadOnlyList<MailInboxExportEntryResponse> result = await client.GetPageAsync(recipient, hasDate ? entry.ReceivedAtUtc : null,
            hasId ? entry.Id : null, CancellationToken.None);

        Assert.Equal(entry, Assert.Single(result));
        string expected = "/api/mail-inbox/export?limit=50&recipient=" + Uri.EscapeDataString(recipient);
        if (hasDate && hasId) {
            expected += "&beforeReceivedAtUtc=" + Uri.EscapeDataString(entry.ReceivedAtUtc.ToString("O", CultureInfo.InvariantCulture)) + "&beforeId=" + entry.Id;
        }
        Assert.Multiple(
            () => Assert.Equal(expected, handler.Path),
            () => Assert.Equal("metadata-test-key", handler.ApiKey),
            () => Assert.Equal(HttpMethod.Get, handler.Method));
    }

    [Fact]
    public async Task GetPageAsync_RejectsNullPayload() {
        using var handler = new ResponseHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") });
        using HttpClient http = CreateHttp(handler);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateClient(http).GetPageAsync("a@test", beforeReceivedAtUtc: null, beforeId: null, CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Export_PropagatesHttpFailure(bool mime) {
        using var handler = new ResponseHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        using HttpClient http = CreateHttp(handler);
        MailInboxExportClient client = CreateClient(http);
        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(async () => {
            if (mime) {
                await client.GetMimeAsync(Guid.NewGuid(), "a@test", CancellationToken.None).ConfigureAwait(false);
            } else {
                await client.GetPageAsync("a@test", beforeReceivedAtUtc: null, beforeId: null, CancellationToken.None).ConfigureAwait(false);
            }
        });
        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
    }

    [Fact]
    public async Task GetMimeAsync_ReturnsNullForMissingContent() {
        using var handler = new ResponseHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        using HttpClient http = CreateHttp(handler);
        Assert.Null(await CreateClient(http).GetMimeAsync(Guid.NewGuid(), "a@test", CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20000)]
    [InlineData(10485760)]
    public async Task GetMimeAsync_PreservesBinaryContentUpToLimit(int length) {
        byte[] bytes = new byte[length];
        new Random(42).NextBytes(bytes);
        using var handler = new ResponseHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        using HttpClient http = CreateHttp(handler);
        var id = Guid.NewGuid();

        byte[]? result = await CreateClient(http).GetMimeAsync(id, "bugs+export@test", CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(bytes, result),
            () => Assert.Equal($"/api/mail-inbox/export/{id}/mime?recipient=bugs%2Bexport%40test", handler.Path),
            () => Assert.Equal("content-test-key", handler.ApiKey));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetMimeAsync_RejectsOversizedKnownOrStreamingContent(bool knownLength) {
        byte[] bytes = new byte[10485761];
        HttpContent content = knownLength ? new ByteArrayContent(bytes) : new UnknownLengthContent(bytes);
        using var handler = new ResponseHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        using HttpClient http = CreateHttp(handler);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateClient(http).GetMimeAsync(Guid.NewGuid(), "a@test", CancellationToken.None));
        Assert.Contains("size limit", exception.Message, StringComparison.Ordinal);
    }

    private static HttpClient CreateHttp(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://inbox.test") };

    private static MailInboxExportClient CreateClient(HttpClient http) => new(http,
        Microsoft.Extensions.Options.Options.Create(new MailInboxClientOptions {
            MetadataApiKey = "metadata-test-key",
            ContentApiKey = "content-test-key",
        }));

    [ExcludeFromCodeCoverage]
    private sealed class ResponseHandler(HttpResponseMessage response) : HttpMessageHandler {
        public string? Path { get; private set; }
        public string? ApiKey { get; private set; }
        public HttpMethod? Method { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            Path = request.RequestUri!.PathAndQuery;
            ApiKey = request.Headers.GetValues("X-MailInbox-Api-Key").Single();
            Method = request.Method;
            return Task.FromResult(response);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class UnknownLengthContent(byte[] bytes) : HttpContent {
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(bytes).AsTask();
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new MemoryStream(bytes));
    }
}
