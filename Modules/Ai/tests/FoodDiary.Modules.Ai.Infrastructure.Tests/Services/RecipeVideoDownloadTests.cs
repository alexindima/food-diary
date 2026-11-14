using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
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

        Error error = ResultAssert.Failure(await CreateProcessor(http).ExtractAudioAsync(video: null, "https://example.org/recipe", CancellationToken.None), "Ai.InvalidRecipeVideoSource");
        Assert.Multiple(() => Assert.Equal(ErrorKind.Validation, error.Kind), () => Assert.Equal(expectedRequests, handler.Targets.Count));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "Ai.InvalidRecipeVideoSource", ErrorKind.Validation, LogLevel.Information, "source_http_404")]
    [InlineData(HttpStatusCode.TooManyRequests, "Ai.RecipeVideoUnavailable", ErrorKind.ExternalFailure, LogLevel.Warning, "upstream_http_429")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Ai.RecipeVideoUnavailable", ErrorKind.ExternalFailure, LogLevel.Warning, "upstream_http_503")]
    [InlineData(null, "Ai.RecipeVideoUnavailable", ErrorKind.ExternalFailure, LogLevel.Warning, "upstream_request_failed")]
    public async Task DownloadFailure_ReportsSafeStructuredReason(
        HttpStatusCode? status, string code, ErrorKind kind, LogLevel level, string reason) {
        const string url = "https://example.org/video?token=private-fixture-value";
        using var handler = new SequenceHandler((_, _) => status is { } responseStatus
            ? new HttpResponseMessage(responseStatus)
            : throw new HttpRequestException($"Network request failed for {url}"));
        using var http = new HttpClient(handler);
        var logger = new RecordingLogger();

        Error error = ResultAssert.Failure(await CreateProcessor(http, logger).ExtractAudioAsync(video: null, url, CancellationToken.None), code);
        (LogLevel Level, IReadOnlyDictionary<string, object?> Fields, string Message, Exception? Exception) entry = Assert.Single(logger.Entries);
        Assert.Multiple(
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Equal(level, entry.Level),
            () => Assert.Equal("download_source", entry.Fields["Stage"]),
            () => Assert.Equal(reason, entry.Fields["Reason"]),
            () => Assert.DoesNotContain("private-fixture-value", entry.Message, StringComparison.Ordinal),
            () => Assert.Null(entry.Exception));
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

    private static RecipeVideoProcessor CreateProcessor(HttpClient http, ILogger<RecipeVideoProcessor>? logger = null) => new(http,
        Options.Create(new RecipeVideoOptions { FfmpegPath = Path.Combine(Path.GetTempPath(), "absent-recipe-processor-" + Guid.NewGuid().ToString("N")) }),
        logger ?? NullLogger<RecipeVideoProcessor>.Instance, new RecipeYouTubeVideoSource(http));

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
    private sealed class RecordingLogger : ILogger<RecipeVideoProcessor> {
        public List<(LogLevel Level, IReadOnlyDictionary<string, object?> Fields, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            IEnumerable<KeyValuePair<string, object?>> fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state);
            Entries.Add((logLevel, fields.ToDictionary(), formatter(state, exception), exception));
        }
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
