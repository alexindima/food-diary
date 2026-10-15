using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RecipeYouTubeVideoSourceTests {
    [Theory]
    [InlineData("https://www.youtube.com/shorts/yNgjOzU3QaU", "yNgjOzU3QaU")]
    [InlineData("https://www.youtube.com/watch?v=ncD_3gaRKbM", "ncD_3gaRKbM")]
    [InlineData("https://youtu.be/yNgjOzU3QaU?si=share", "yNgjOzU3QaU")]
    public async Task PublicVideo_DownloadsAudioWithoutAnUploadedFile_AndPreservesIdCase(string url, string expectedId) {
        using var handler = new YouTubeFixtureHandler();
        using var gate = new RecipeYouTubeHttpHandler { InnerHandler = handler };
        using var http = new HttpClient(gate);
        await using var destination = new MemoryStream();

        await new RecipeYouTubeVideoSource(http).DownloadAsync(new Uri(url), destination, CancellationToken.None);

        Assert.Equal(handler.Audio, destination.ToArray());
        Assert.Equal(expectedId, handler.VideoId);
        Assert.True(handler.MediaRequests > 0);
        Assert.All(handler.Requests, request => Assert.True(request.StartsWith("https://www.youtube.com/", StringComparison.Ordinal) ||
            request.StartsWith("https://media.googlevideo.com/", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(301)]
    [InlineData(0)]
    public async Task InvalidDuration_IsRejectedBeforeDownloadingAudio(int duration) {
        using var handler = new YouTubeFixtureHandler { Duration = duration };
        using var http = new HttpClient(handler);
        await using var destination = new MemoryStream();

        await Assert.ThrowsAsync<InvalidDataException>(() => new RecipeYouTubeVideoSource(http).DownloadAsync(
            new Uri("https://www.youtube.com/watch?v=ncD_3gaRKbM"), destination, CancellationToken.None));

        Assert.Equal(0, handler.MediaRequests);
        Assert.Equal(0, destination.Length);
    }

    [Fact]
    public async Task OversizedAudio_IsRejectedBeforeCopying() {
        using var handler = new YouTubeFixtureHandler { DeclaredAudioSize = (50 * 1024 * 1024) + 1 };
        using var http = new HttpClient(handler);
        await using var destination = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => new RecipeYouTubeVideoSource(http).DownloadAsync(
            new Uri("https://youtu.be/yNgjOzU3QaU"), destination, CancellationToken.None));
        Assert.Equal(0, destination.Length);
    }

    [Theory]
    [InlineData("https://www.youtube.com.evil.test/watch?v=ncD_3gaRKbM")]
    [InlineData("https://youtube.com/@channel")]
    [InlineData("https://youtu.be/invalid")]
    public async Task InvalidOrLookalikeSource_DoesNotMakeRequests(string url) {
        using var handler = new YouTubeFixtureHandler();
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => new RecipeYouTubeVideoSource(http).DownloadAsync(
            new Uri(url), Stream.Null, CancellationToken.None));
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("http://www.youtube.com/watch?v=ncD_3gaRKbM")]
    [InlineData("https://www.youtube.com:444/watch?v=ncD_3gaRKbM")]
    [InlineData("https://user@www.youtube.com/watch?v=ncD_3gaRKbM")]
    [InlineData("https://127.0.0.1/private")]
    [InlineData("https://media.googlevideo.com.evil.test/audio")]
    [InlineData("https://evil.test/audio")]
    public async Task DestinationGate_RejectsUnsafeRequestsBeforeTransport(string url) {
        using var handler = new YouTubeFixtureHandler();
        using var gate = new RecipeYouTubeHttpHandler { InnerHandler = handler };
        using var http = new HttpClient(gate);
        await Assert.ThrowsAsync<HttpRequestException>(() => http.GetAsync(url));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DestinationGate_BoundsMetadataResponses() {
        using var handler = new YouTubeFixtureHandler { OversizedMetadata = true };
        using var gate = new RecipeYouTubeHttpHandler { InnerHandler = handler };
        using var http = new HttpClient(gate);
        await Assert.ThrowsAsync<HttpRequestException>(() => http.GetAsync("https://www.youtube.com/watch?v=ncD_3gaRKbM"));
    }

    [Theory]
    [InlineData("http://www.youtube.com/watch?v=ncD_3gaRKbM")]
    [InlineData("https://127.0.0.1/private")]
    [InlineData("https://media.googlevideo.com.evil.test/audio")]
    public async Task DestinationGate_RevalidatesRedirectsBeforeFollowing(string target) {
        using var handler = new YouTubeFixtureHandler { RedirectTarget = target };
        using var gate = new RecipeYouTubeHttpHandler { InnerHandler = handler };
        using var http = new HttpClient(gate);
        await Assert.ThrowsAsync<HttpRequestException>(() => http.GetAsync("https://www.youtube.com/watch?v=ncD_3gaRKbM"));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task DestinationGate_FollowsPublicMediaRedirects() {
        using var handler = new YouTubeFixtureHandler { RedirectTarget = "https://media.googlevideo.com/audio" };
        using var gate = new RecipeYouTubeHttpHandler { InnerHandler = handler };
        using var http = new HttpClient(gate);
        byte[] audio = await http.GetByteArrayAsync("https://www.youtube.com/watch?v=ncD_3gaRKbM");
        Assert.Equal(handler.Audio, audio);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task PrivateVideo_ReturnsControlledSourceFailure() {
        using var handler = new YouTubeFixtureHandler { Unavailable = true };
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => new RecipeYouTubeVideoSource(http).DownloadAsync(
            new Uri("https://www.youtube.com/watch?v=ncD_3gaRKbM"), Stream.Null, CancellationToken.None));
        Assert.Equal(0, handler.MediaRequests);
    }

    [Fact]
    public async Task Cancellation_IsPropagated() {
        using var handler = new YouTubeFixtureHandler();
        using var http = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new RecipeYouTubeVideoSource(http).DownloadAsync(
            new Uri("https://youtu.be/yNgjOzU3QaU"), Stream.Null, cancellation.Token));
    }

    [ExcludeFromCodeCoverage]
    private sealed class YouTubeFixtureHandler : HttpMessageHandler {
        public byte[] Audio { get; } = [.. Enumerable.Range(0, 32).Select(value => (byte)value)];
        public List<string> Requests { get; } = [];
        public string? VideoId { get; private set; }
        public int MediaRequests { get; private set; }
        public int Duration { get; init; } = 30;
        public long DeclaredAudioSize { get; init; } = 32;
        public bool OversizedMetadata { get; init; }
        public bool Unavailable { get; init; }
        public string? RedirectTarget { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            Uri url = request.RequestUri!;
            Requests.Add(url.AbsoluteUri);
            if (RedirectTarget is not null && Requests.Count == 1) {
                var redirect = new HttpResponseMessage(HttpStatusCode.Found);
                redirect.Headers.Location = new Uri(RedirectTarget);
                return redirect;
            }
            if (string.Equals(url.Host, "media.googlevideo.com", StringComparison.Ordinal)) {
                MediaRequests++;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Audio) };
            }
            if (string.Equals(url.AbsolutePath, "/sw.js_data", StringComparison.Ordinal)) {
                object?[] visitor = new object?[14];
                visitor[13] = "fixture-visitor";
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = JsonContent.Create(new[] { new object?[] { null, null, new[] { new[] { visitor } } } }),
                };
            }
            if (request.Content is not null) {
                using var payload = JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
                VideoId = payload.RootElement.GetProperty("videoId").GetString();
            }
            string player = JsonSerializer.Serialize(new {
                playabilityStatus = new { status = "OK" },
                videoDetails = new { videoId = VideoId, title = "Fixture recipe", author = "Fixture cook", channelId = "UC0000000000000000000000", lengthSeconds = Duration.ToString(CultureInfo.InvariantCulture) },
                microformat = new { playerMicroformatRenderer = new { uploadDate = "2026-01-01" } },
                streamingData = new {
                    adaptiveFormats = new[] { new {
                    itag = 140, url = "https://media.googlevideo.com/audio?fixture=1", mimeType = "audio/mp4; codecs=\"mp4a.40.2\"",
                    bitrate = 64000, contentLength = DeclaredAudioSize.ToString(CultureInfo.InvariantCulture),
                }, },
                },
            });
            StringContent content = string.Equals(url.AbsolutePath, "/watch", StringComparison.Ordinal)
                ? new StringContent((Unavailable ? "" : "<meta property='og:url' content='https://www.youtube.com/watch'>") +
                    "<div id='player'></div><script>var ytInitialPlayerResponse = " + player + ";</script>")
                : new StringContent(player);
            if (OversizedMetadata) {
                content.Headers.ContentLength = (4 * 1024 * 1024) + 1;
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }
    }
}
