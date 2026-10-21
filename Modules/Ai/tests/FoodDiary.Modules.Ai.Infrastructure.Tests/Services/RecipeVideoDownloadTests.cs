using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
[Collection("Recipe media")]
public sealed class RecipeVideoDownloadTests {
    [Theory]
    [InlineData(null, "Ai.InvalidRecipeVideo")]
    [InlineData("http://example.org/video", "Ai.InvalidRecipeUrl")]
    public async Task InvalidSource_IsRejectedBeforeSendingRequests(string? url, string code) {
        using var handler = new SequenceHandler((_, _) => throw new InvalidOperationException("No request expected."));
        using var http = new HttpClient(handler);

        ResultAssert.Failure(await CreateProcessor(http).ExtractAudioAsync(video: null, url, CancellationToken.None), code);
        Assert.Empty(handler.Targets);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupportedContainer_ReportsAnUnavailableProcessorWithoutLeakingAnException(bool webm) {
        byte[] header = webm ? [0x1a, 0x45, 0xdf, 0xa3, 0, 0, 0, 0, 0, 0, 0, 0] : Encoding.ASCII.GetBytes("0000ftyp0000");
        await using var video = new MemoryStream(header);
        using var http = new HttpClient();

        ResultAssert.Failure(await CreateProcessor(http).ExtractAudioAsync(video, sourceUrl: null, CancellationToken.None), "Ai.RecipeVideoUnavailable");
    }

    [Fact]
    public async Task HtmlMetadata_ResolvesRelativeVideoUrlAndDownloadsTheContainer() {
        using var handler = new SequenceHandler((index, _) => index == 0
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<video><source src='/media/video.mp4'></video>", Encoding.UTF8, "text/html") }
            : VideoResponse("video/mp4"));
        using var http = new HttpClient(handler);

        ResultAssert.Failure(await CreateProcessor(http).ExtractAudioAsync(video: null, "https://example.org/recipe", CancellationToken.None), "Ai.RecipeVideoUnavailable");
        Assert.Equal(new[] { "https://example.org/recipe", "https://example.org/media/video.mp4" }, handler.Targets.Select(uri => uri.AbsoluteUri), StringComparer.Ordinal);
    }

    [Fact]
    public async Task Redirect_ResolvesAnAllowedRelativeTarget() {
        using var handler = new SequenceHandler((index, _) => index == 0
            ? new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("/media/video.mp4", UriKind.Relative) } }
            : VideoResponse("video/mp4"));
        using var http = new HttpClient(handler);

        ResultAssert.Failure(await CreateProcessor(http).ExtractAudioAsync(video: null, "https://example.org/recipe", CancellationToken.None), "Ai.RecipeVideoUnavailable");
        Assert.Equal(2, handler.Targets.Count);
        Assert.Equal("https://example.org/media/video.mp4", handler.Targets[1].AbsoluteUri);
    }

    [Theory]
    [InlineData("missing-location", 1)]
    [InlineData("no-video", 1)]
    [InlineData("http-video", 1)]
    [InlineData("unsupported-type", 1)]
    [InlineData("oversized", 1)]
    [InlineData("status", 1)]
    [InlineData("redirect-loop", 5)]
    public async Task UnusableSource_ReturnsControlledFailureWithBoundedRequests(string failure, int expectedRequests) {
        using var handler = new SequenceHandler((_, _) => failure switch {
            "missing-location" => new HttpResponseMessage(HttpStatusCode.Found),
            "no-video" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<p>No media</p>", Encoding.UTF8, "text/html") },
            "http-video" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<video src='http://127.0.0.1/private'></video>", Encoding.UTF8, "text/html") },
            "unsupported-type" => VideoResponse("text/plain"),
            "oversized" => VideoResponse("video/mp4", RecipeVideoProcessor.MaximumVideoBytes + 1),
            "status" => new HttpResponseMessage(HttpStatusCode.NotFound),
            "redirect-loop" => new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("/again", UriKind.Relative) } },
            _ => throw new ArgumentOutOfRangeException(nameof(failure)),
        });
        using var http = new HttpClient(handler);

        ResultAssert.Failure(await CreateProcessor(http).ExtractAudioAsync(video: null, "https://example.org/recipe", CancellationToken.None), "Ai.RecipeVideoUnavailable");
        Assert.Equal(expectedRequests, handler.Targets.Count);
    }

    [Fact]
    public async Task DownloadedIncompleteContainer_ReturnsValidationFailure() {
        using var handler = new SequenceHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new ByteArrayContent([1, 2, 3]) { Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") } },
        });
        using var http = new HttpClient(handler);

        ResultAssert.Failure(await CreateProcessor(http).ExtractAudioAsync(video: null, "https://example.org/video", CancellationToken.None), "Ai.InvalidRecipeVideo");
        Assert.Single(handler.Targets);
    }

    private static RecipeVideoProcessor CreateProcessor(HttpClient http) => new(http,
        Options.Create(new RecipeVideoOptions { FfmpegPath = Path.Combine(Path.GetTempPath(), "absent-recipe-processor-" + Guid.NewGuid().ToString("N")) }),
        NullLogger<RecipeVideoProcessor>.Instance, new RecipeYouTubeVideoSource(http));

    private static HttpResponseMessage VideoResponse(string mediaType, long? length = null) {
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new ByteArrayContent(Encoding.ASCII.GetBytes("0000ftyp0000")),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        if (length is not null) {
            response.Content.Headers.ContentLength = length;
        }
        return response;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SequenceHandler(Func<int, HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler {
        public List<Uri> Targets { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Targets.Add(request.RequestUri!);
            return Task.FromResult(respond(Targets.Count - 1, request));
        }
    }
}
