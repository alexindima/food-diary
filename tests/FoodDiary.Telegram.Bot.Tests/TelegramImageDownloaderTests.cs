using System.Net;
using System.Net.Http.Json;
using FoodDiary.Telegram.Bot.Images;
using Telegram.Bot;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramImageDownloaderTests {
    [Theory]
    [InlineData("image/jpeg", "FFD8FF01")]
    [InlineData("image/png", "89504E470D0A1A0A")]
    [InlineData("image/webp", "524946460400000057454250")]
    public async Task DownloadAsync_ReturnsVerifiedBytes(string contentType, string hex) {
        byte[] bytes = Convert.FromHexString(hex);
        using var handler = new DownloadHandler(bytes, bytes.Length, "photos/food");
        using var http = new HttpClient(handler);
        var downloader = new TelegramImageDownloader(new TelegramBotClient("123:test", http));

        byte[] result = await downloader.DownloadAsync(new TelegramImageSelection("food", contentType, bytes.Length), CancellationToken.None);

        Assert.Equal(bytes, result);
        Assert.Equal(1, handler.Downloads);
    }

    [Theory]
    [InlineData(0, "photos/food")]
    [InlineData(-1, "photos/food")]
    [InlineData(20971521, "photos/food")]
    [InlineData(3, null)]
    [InlineData(3, " ")]
    public async Task DownloadAsync_RejectsInvalidMetadataBeforeDownloading(long size, string? path) {
        using var handler = new DownloadHandler([255, 216, 255], size, path);
        using var http = new HttpClient(handler);
        var downloader = new TelegramImageDownloader(new TelegramBotClient("123:test", http));

        await Assert.ThrowsAsync<InvalidDataException>(() => downloader.DownloadAsync(
            new TelegramImageSelection("food", "image/jpeg", DeclaredSizeBytes: null), CancellationToken.None));

        Assert.Equal(0, handler.Downloads);
    }

    [Fact]
    public async Task DownloadAsync_RejectsContentThatDoesNotMatchTheDeclaredMimeType() {
        using var handler = new DownloadHandler("not an image"u8.ToArray(), 12, "photos/food");
        using var http = new HttpClient(handler);
        var downloader = new TelegramImageDownloader(new TelegramBotClient("123:test", http));
        await Assert.ThrowsAsync<InvalidDataException>(() => downloader.DownloadAsync(
            new TelegramImageSelection("food", "image/jpeg", DeclaredSizeBytes: null), CancellationToken.None));
        Assert.False(TelegramImageDownloader.HasExpectedSignature([255, 216, 255], "image/gif"));
        Assert.False(TelegramImageDownloader.HasExpectedSignature("RIFF0000FAKE"u8, "image/webp"));
    }

    [Fact]
    public async Task DownloadAsync_EnforcesActualSizeEvenWhenMetadataUnderstatesIt() {
        byte[] oversized = new byte[TelegramImageSelector.MaximumFileBytes + 1];
        using var handler = new DownloadHandler(oversized, 3, "photos/food");
        using var http = new HttpClient(handler);
        var downloader = new TelegramImageDownloader(new TelegramBotClient("123:test", http));
        await Assert.ThrowsAsync<InvalidDataException>(() => downloader.DownloadAsync(
            new TelegramImageSelection("food", "image/jpeg", DeclaredSizeBytes: null), CancellationToken.None));
    }

    [ExcludeFromCodeCoverage]
    private sealed class DownloadHandler(byte[] bytes, long size, string? path) : HttpMessageHandler {
        public int Downloads { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.RequestUri!.AbsolutePath.EndsWith("/getFile", StringComparison.OrdinalIgnoreCase)) {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = JsonContent.Create(new { ok = true, result = new { file_id = "food", file_unique_id = "unique", file_size = size, file_path = path } }),
                });
            }
            Assert.EndsWith("/photos/food", request.RequestUri.AbsolutePath, StringComparison.Ordinal);
            Downloads++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        }
    }
}
