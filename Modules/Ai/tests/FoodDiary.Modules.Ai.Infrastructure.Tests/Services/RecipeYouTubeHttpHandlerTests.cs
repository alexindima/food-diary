using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RecipeYouTubeHttpHandlerTests {
    [Theory]
    [InlineData(HttpStatusCode.TemporaryRedirect, true)]
    [InlineData(HttpStatusCode.PermanentRedirect, true)]
    [InlineData(HttpStatusCode.Found, false)]
    public async Task Redirects_PreserveOnlyApprovedHeadersAndTheRequiredBodySemantics(HttpStatusCode status, bool keepBody) {
        const string body = "{\"videoId\":\"fixture\"}";
        using var transport = new RecordingHandler(index => index == 0
            ? new HttpResponseMessage(status) { Headers = { Location = new Uri("https://media.googlevideo.com/audio") } }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        using var gate = new RecipeYouTubeHttpHandler { InnerHandler = transport };
        using var invoker = new HttpMessageInvoker(gate);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/watch") {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-test-value");
        request.Headers.Range = new RangeHeaderValue(0, 2);
        request.Headers.UserAgent.ParseAdd("RecipeFixture/1.0");

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(2, transport.Requests.Count);
        RequestSnapshot redirected = transport.Requests[1];
        Assert.Multiple(
            () => Assert.Equal(keepBody ? HttpMethod.Post : HttpMethod.Get, redirected.Method),
            () => Assert.Equal(keepBody ? body : null, redirected.Body),
            () => Assert.Null(redirected.Authorization),
            () => Assert.Equal("bytes=0-2", redirected.Range),
            () => Assert.Equal("RecipeFixture/1.0", redirected.UserAgent));
        Assert.Equal(body, await request.Content.ReadAsStringAsync());
        Assert.Equal(new byte[] { 1, 2, 3 }, await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 4)]
    public async Task Redirects_RejectMissingLocationsAndBoundRedirectLoops(bool loop, int expectedRequests) {
        using var transport = new RecordingHandler(_ => {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            if (loop) { response.Headers.Location = new Uri("https://www.youtube.com/again"); }
            return response;
        });
        using var gate = new RecipeYouTubeHttpHandler { InnerHandler = transport };
        using var invoker = new HttpMessageInvoker(gate);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.youtube.com/watch");

        await Assert.ThrowsAsync<HttpRequestException>(() => invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(expectedRequests, transport.Requests.Count);
    }

    [Fact]
    public async Task MetadataReadFailure_DisposesTheResponseAndPreservesTheFailure() {
        using var content = new UnreadableContent();
        using var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        using var gate = new RecipeYouTubeHttpHandler { InnerHandler = transport };
        using var invoker = new HttpMessageInvoker(gate);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.youtube.com/watch");

        await Assert.ThrowsAsync<HttpRequestException>(() => invoker.SendAsync(request, CancellationToken.None));

        Assert.True(content.Disposed);
        Assert.Single(transport.Requests);
    }

    [ExcludeFromCodeCoverage]
    private sealed record RequestSnapshot(HttpMethod Method, string? Body, string? Authorization, string? Range, string UserAgent);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHandler(Func<int, HttpResponseMessage> respond) : HttpMessageHandler {
        public List<RequestSnapshot> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Requests.Add(new RequestSnapshot(request.Method,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.Authorization?.ToString(), request.Headers.Range?.ToString(), request.Headers.UserAgent.ToString()));
            return respond(Requests.Count - 1);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class UnreadableContent : HttpContent {
        public bool Disposed { get; private set; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.FromException(new IOException("Metadata read failed."));
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
}
