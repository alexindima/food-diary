namespace FoodDiary.Telegram.Bot.Images;

internal sealed class BoundedImageStream(int maximumBytes) : Stream {
    private readonly MemoryStream _content = new();
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _content.Length;
    public override long Position { get => _content.Position; set => throw new NotSupportedException(); }

    internal byte[] ToArray() => _content.ToArray();

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    public override void Write(ReadOnlySpan<byte> buffer) => Append(buffer);

    private void Append(ReadOnlySpan<byte> buffer) {
        if (buffer.Length > maximumBytes - _content.Length) {
            throw new InvalidDataException("Telegram image exceeds the download limit.");
        }
        _content.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        Append(buffer.AsSpan(offset, count));
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        Append(buffer.Span);
        return ValueTask.CompletedTask;
    }

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _content.Dispose();
        }
        base.Dispose(disposing);
    }
}
