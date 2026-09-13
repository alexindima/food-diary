using FoodDiary.Telegram.Bot.Images;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BoundedImageStreamTests {
    [Fact]
    public void StreamContract_SynchronousWritesRespectTheBound() {
        using var stream = new BoundedImageStream(6);
        Assert.Multiple(
            () => Assert.False(stream.CanRead),
            () => Assert.False(stream.CanSeek),
            () => Assert.True(stream.CanWrite),
            () => Assert.Equal(0, stream.Position));
        stream.Write([0, 1, 2, 0], 1, 2);
        stream.Write([3]);
        stream.Write([4, 5]);
        stream.WriteByte(6);
        stream.Flush();
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, stream.ToArray());
        Assert.Equal(6, stream.Position);
        Assert.Throws<InvalidDataException>(() => stream.Write([7], 0, 1));
        Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
    }

    [Fact]
    public async Task WriteAsync_ArrayOverloadHonorsOffsetAndCancellation() {
        await using var stream = new BoundedImageStream(2);
        await stream.WriteAsync([0, 4, 5, 0], 1, 2, CancellationToken.None);
        Assert.Equal(new byte[] { 4, 5 }, stream.ToArray());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.WriteAsync(new byte[1], 0, 1, cancellation.Token));
        Assert.Equal(2, stream.Length);
    }

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
