using System.Buffers;
using System.Text;
using System.Text.Json;

namespace FoodDiary.Integrations.Http;

internal static class BoundedHttpContentReader {
    public const long DefaultMaxResponseBodyBytes = 1024 * 1024;
    public const int DefaultJsonMaxDepth = 32;
    public const int DefaultErrorSummaryCharacters = 200;
    public static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromSeconds(15);

    public static async Task<string> ReadAsStringAsync(
        HttpContent content,
        long maxBytes,
        TimeSpan readTimeout,
        CancellationToken cancellationToken) {
        byte[] bytes = await ReadAsByteArrayAsync(content, maxBytes, readTimeout, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

    public static async Task<T?> ReadFromJsonAsync<T>(
        HttpContent content,
        JsonSerializerOptions options,
        long maxBytes,
        TimeSpan readTimeout,
        CancellationToken cancellationToken) {
        byte[] bytes = await ReadAsByteArrayAsync(content, maxBytes, readTimeout, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(bytes, options);
    }

    public static async Task<byte[]> ReadAsByteArrayAsync(
        HttpContent content,
        long maxBytes,
        TimeSpan readTimeout,
        CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(readTimeout, TimeSpan.Zero);

        long? declaredLength = content.Headers.ContentLength;
        if (declaredLength is not null && declaredLength.Value > maxBytes) {
            throw CreateTooLargeException();
        }

        using var readDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readDeadline.CancelAfter(readTimeout);
        try {
            Stream stream = await content.ReadAsStreamAsync(readDeadline.Token).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false)) {
                int capacity = declaredLength is > 0 and <= int.MaxValue ? (int)declaredLength.Value : 0;
                var buffer = new MemoryStream(capacity);
                await using (buffer.ConfigureAwait(false)) {
                    byte[] rented = ArrayPool<byte>.Shared.Rent(81920);
                    try {
                        while (true) {
                            int read = await stream.ReadAsync(rented.AsMemory(), readDeadline.Token).ConfigureAwait(false);
                            if (read == 0) {
                                break;
                            }

                            if (buffer.Length > maxBytes - read) {
                                throw CreateTooLargeException();
                            }

                            await buffer.WriteAsync(rented.AsMemory(0, read), readDeadline.Token).ConfigureAwait(false);
                        }

                        return buffer.ToArray();
                    } finally {
                        ArrayPool<byte>.Shared.Return(rented, clearArray: true);
                    }
                }
            }
        } catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested) {
            throw new TimeoutException("Upstream response body exceeded the configured read deadline.", exception);
        }
    }

    public static string NormalizeErrorSummary(string value) {
        string compact = value.ReplaceLineEndings(" ").Trim();
        var builder = new StringBuilder(Math.Min(compact.Length, DefaultErrorSummaryCharacters));
        foreach (char character in compact) {
            if (!char.IsControl(character)) {
                builder.Append(character);
            }

            if (builder.Length == DefaultErrorSummaryCharacters) {
                break;
            }
        }

        return builder.Length == 0 ? "upstream error body unavailable" : builder.ToString();
    }

    private static InvalidDataException CreateTooLargeException() =>
        new("Upstream response body exceeded the configured byte limit.");
}
