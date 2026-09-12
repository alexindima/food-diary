using FoodDiary.Telegram.Bot.Images;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BoundedImageStreamTests {
    [Fact]
    public async Task WriteAsync_RejectsOverflowBeforeAppendingBytes() {
        await using var stream = new BoundedImageStream(4);
        await stream.WriteAsync(new byte[] { 1, 2, 3 }.AsMemory(), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await stream.WriteAsync(new byte[] { 4, 5 }.AsMemory(), CancellationToken.None));
        Assert.Equal(new byte[] { 1, 2, 3 }, stream.ToArray());
        stream.WriteByte(4);
        Assert.Equal(4, stream.Length);
        Assert.Throws<InvalidDataException>(() => stream.WriteByte(5));
    }

    [Fact]
    public async Task WriteAsync_CancellationDoesNotAppendContent() {
        await using var stream = new BoundedImageStream(4);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await stream.WriteAsync(new byte[] { 1 }.AsMemory(), cancelled.Token));
        Assert.Empty(stream.ToArray());
    }

    [Fact]
    public void SignatureCheck_RejectsMimeSpoofingAndTruncatedHeaders() {
        byte[] png = [137, 80, 78, 71, 13, 10, 26, 10];
        Assert.True(TelegramImageDownloader.HasExpectedSignature(png, "image/png"));
        Assert.False(TelegramImageDownloader.HasExpectedSignature(png, "image/jpeg"));
        Assert.False(TelegramImageDownloader.HasExpectedSignature(png.AsSpan(0, 3), "image/png"));
        Assert.False(TelegramImageDownloader.HasExpectedSignature("food.jpg"u8, "image/jpeg"));
    }
}
