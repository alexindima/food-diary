using System.Diagnostics;
using System.Net;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RecipeVideoProcessorTests {
    [Theory]
    [InlineData("<meta content='https://example.org/v.mp4?a=1&amp;b=2' property='og:video'>", "https://example.org/v.mp4?a=1&b=2")]
    [InlineData("<video><source src='/v.webm'></video>", "/v.webm")]
    [InlineData("<meta property='og:image' content='photo.jpg'>", null)]
    public void VideoUrl_UsesVideoMetadataAndDecodesEntities(string html, string? expected) =>
        Assert.Equal(expected, RecipeVideoProcessor.FindVideoUrl(html));

    [Fact]
    public async Task InvalidContainer_IsRejectedBeforeStartingFfmpeg() {
        using var http = new HttpClient();
        RecipeVideoProcessor processor = CreateProcessor(http, "nonexistent-ffmpeg-test");
        await using var video = new MemoryStream(new byte[20]);
        ResultAssert.Failure(await processor.ExtractAudioAsync(video, sourceUrl: null, CancellationToken.None), "Ai.InvalidRecipeVideo");
    }

    [Fact]
    public async Task Redirect_ToHttp_IsRejectedWithoutFollowingIt() {
        using var handler = new RedirectHandler();
        using var http = new HttpClient(handler);
        ResultAssert.Failure(await CreateProcessor(http, "nonexistent-ffmpeg-test").ExtractAudioAsync(video: null, "https://example.org/video", CancellationToken.None));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Cancellation_IsPropagatedBeforeMediaProcessing() {
        using var http = new HttpClient();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateProcessor(http, "nonexistent-ffmpeg-test").ExtractAudioAsync(video: null, "https://example.org/video", cancellation.Token));
    }

    [RequiresFfmpegFact]
    public async Task Ffmpeg_ExtractsMonoWavFromMp4_AndRemovesTemporaryMedia() {
        string[] before = Directory.GetDirectories(Path.GetTempPath(), "fooddiary-recipe-*");
        byte[] video = await GenerateVideoAsync(audio: true, duration: 1, CancellationToken.None);
        await using var input = new MemoryStream(video);
        using var http = new HttpClient();
        RecipeAudio audio = ResultAssert.Success(await CreateProcessor(http, FfmpegPath).ExtractAudioAsync(input, "https://example.org/source", CancellationToken.None));
        Assert.Multiple(
            () => Assert.InRange(audio.DurationSeconds, 0.9, 1.1),
            () => Assert.InRange(audio.Wav.Length, 30000, 40000),
            () => Assert.Equal(audio.DurationSeconds, RecipeVideoProcessor.ReadDuration(audio.Wav)),
            () => Assert.Equal("https://example.org/source", audio.SourceUrl),
            () => Assert.Empty(Directory.GetDirectories(Path.GetTempPath(), "fooddiary-recipe-*").Except(before, StringComparer.Ordinal)));
    }

    [RequiresFfmpegFact]
    public async Task Ffmpeg_VideoWithoutAudio_ReturnsValidationErrorAndCleansUp() {
        string[] before = Directory.GetDirectories(Path.GetTempPath(), "fooddiary-recipe-*");
        await using var video = new MemoryStream(await GenerateVideoAsync(audio: false, duration: 1, CancellationToken.None));
        using var http = new HttpClient();
        ResultAssert.Failure(await CreateProcessor(http, FfmpegPath).ExtractAudioAsync(video, sourceUrl: null, CancellationToken.None), "Ai.InvalidRecipeVideo");
        Assert.Empty(Directory.GetDirectories(Path.GetTempPath(), "fooddiary-recipe-*").Except(before, StringComparer.Ordinal));
    }

    [RequiresFfmpegFact]
    public async Task Ffmpeg_AudioOverFiveMinutes_IsRejectedInsteadOfSilentlyTruncated() {
        await using var video = new MemoryStream(await GenerateVideoAsync(audio: true, duration: 302, CancellationToken.None));
        using var http = new HttpClient();
        ResultAssert.Failure(await CreateProcessor(http, FfmpegPath).ExtractAudioAsync(video, sourceUrl: null, CancellationToken.None), "Ai.InvalidRecipeVideo");
    }

    private static string FfmpegPath => Environment.GetEnvironmentVariable("FOODDIARY_TEST_FFMPEG")!;
    private static RecipeVideoProcessor CreateProcessor(HttpClient http, string ffmpeg) => new(http,
        Options.Create(new RecipeVideoOptions { FfmpegPath = ffmpeg }), NullLogger<RecipeVideoProcessor>.Instance);

    private static async Task<byte[]> GenerateVideoAsync(bool audio, int duration, CancellationToken cancellationToken) {
        string path = Path.Combine(Path.GetTempPath(), "fooddiary-fixture-" + Guid.NewGuid().ToString("N") + ".mp4");
        try {
            var start = new ProcessStartInfo(FfmpegPath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            List<string> arguments = ["-nostdin", "-v", "error", "-f", "lavfi", "-i", "color=c=black:s=16x16:r=1"];
            if (audio) {
                arguments.AddRange(["-f", "lavfi", "-i", "sine=frequency=440:sample_rate=48000", "-ac", "2", "-c:a", "aac"]);
            }
            arguments.AddRange(["-t", duration.ToString(System.Globalization.CultureInfo.InvariantCulture), "-c:v", "libx264", "-threads", "1", "-pix_fmt", "yuv420p", path]);
            foreach (string argument in arguments) {
                start.ArgumentList.Add(argument);
            }
            using Process process = Process.Start(start)!;
            Task<string> errors = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            Assert.Equal(0, process.ExitCode);
            Assert.Empty(await errors);
            return await File.ReadAllBytesAsync(path, cancellationToken);
        } finally {
            File.Delete(path);
        }
    }

    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method)]
    private sealed class RequiresFfmpegFactAttribute : FactAttribute {
        public RequiresFfmpegFactAttribute() {
            if (string.IsNullOrWhiteSpace(FfmpegPath) || !File.Exists(FfmpegPath)) {
                Skip = "Set FOODDIARY_TEST_FFMPEG to a local FFmpeg executable to run real media extraction tests.";
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RedirectHandler : HttpMessageHandler {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Calls++;
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri("http://127.0.0.1/video");
            return Task.FromResult(response);
        }
    }
}
