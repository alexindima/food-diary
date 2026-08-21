using FoodDiary.Integrations.Http;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class BoundedHttpContentReaderTests {
    [Fact]
    public async Task ReadAsByteArrayAsync_WhenBodyFits_ReturnsAllBytes() {
        using var content = new ByteArrayContent("hello"u8.ToArray());

        byte[] result = await BoundedHttpContentReader.ReadAsByteArrayAsync(
            content,
            maxBytes: 5,
            readTimeout: TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal("hello"u8.ToArray(), result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReadAsByteArrayAsync_WhenMaximumSizeIsInvalid_Throws(long maxBytes) {
        using var content = new ByteArrayContent([]);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            BoundedHttpContentReader.ReadAsByteArrayAsync(
                content,
                maxBytes,
                TimeSpan.FromSeconds(1),
                CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsByteArrayAsync_WhenTimeoutIsInvalid_Throws() {
        using var content = new ByteArrayContent([]);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            BoundedHttpContentReader.ReadAsByteArrayAsync(
                content,
                maxBytes: 1,
                readTimeout: TimeSpan.Zero,
                CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsByteArrayAsync_WhenDeclaredLengthExceedsLimit_RejectsBeforeReading() {
        using var content = new ByteArrayContent(new byte[11]);

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            BoundedHttpContentReader.ReadAsByteArrayAsync(
                content,
                maxBytes: 10,
                TimeSpan.FromSeconds(1),
                CancellationToken.None));

        Assert.Contains("byte limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadAsByteArrayAsync_WhenStreamedContentExceedsLimit_StopsAtLimit() {
        using var content = new StreamContent(new NonSeekableReadStream(new byte[11]));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            BoundedHttpContentReader.ReadAsByteArrayAsync(
                content,
                maxBytes: 10,
                TimeSpan.FromSeconds(1),
                CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsByteArrayAsync_WhenBodyStalls_ThrowsTimeoutException() {
        using var content = new StreamContent(new BlockingReadStream());

        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            BoundedHttpContentReader.ReadAsByteArrayAsync(
                content,
                maxBytes: 10,
                TimeSpan.FromMilliseconds(10),
                CancellationToken.None));

        Assert.Contains("read deadline", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadAsByteArrayAsync_WhenCallerCancels_PreservesCancellation() {
        using var content = new StreamContent(new BlockingReadStream());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            BoundedHttpContentReader.ReadAsByteArrayAsync(
                content,
                maxBytes: 10,
                TimeSpan.FromSeconds(1),
                cancellation.Token));
    }

    [ExcludeFromCodeCoverage]
    private sealed class BlockingReadStream : Stream {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NonSeekableReadStream(byte[] bytes) : Stream {
        private int _position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _position; set => throw new NotSupportedException(); }
        public override void Flush() {
        }
        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));
        public override int Read(Span<byte> buffer) {
            int count = Math.Min(buffer.Length, bytes.Length - _position);
            bytes.AsSpan(_position, count).CopyTo(buffer);
            _position += count;
            return count;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Read(buffer.Span));
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
