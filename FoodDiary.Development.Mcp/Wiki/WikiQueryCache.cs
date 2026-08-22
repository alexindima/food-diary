using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Development.Mcp.Wiki;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class WikiQueryCache(
    TimeProvider timeProvider,
    WikiRuntimeTelemetry telemetry) {
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromMinutes(2);
    private const int MaximumEntries = 128;
    private const int MaximumCacheableOutputCharacters = 1024 * 1024;
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _insertionOrder = new();

    public bool TryGet(
        string snapshotFingerprint,
        string command,
        IReadOnlyList<string> arguments,
        out WikiCommandResult? result) {
        string key = CreateKey(snapshotFingerprint, command, arguments);
        if (_entries.TryGetValue(key, out CacheEntry? entry)) {
            if (timeProvider.GetUtcNow() - entry.CreatedAtUtc <= EntryLifetime) {
                telemetry.RecordCacheHit();
                result = entry.Result;
                return true;
            }
            _entries.TryRemove(key, out _);
        }

        telemetry.RecordCacheMiss();
        result = null;
        return false;
    }

    public void Set(
        string snapshotFingerprint,
        string command,
        IReadOnlyList<string> arguments,
        WikiCommandResult result) {
        if (result.RawOutput?.Length > MaximumCacheableOutputCharacters) {
            return;
        }

        string key = CreateKey(snapshotFingerprint, command, arguments);
        CacheEntry entry = new(result, timeProvider.GetUtcNow());
        if (_entries.TryAdd(key, entry)) {
            _insertionOrder.Enqueue(key);
        } else {
            _entries[key] = entry;
        }
        Trim();
    }

    public WikiRuntimeMetrics CaptureMetrics() {
        PruneExpired();
        return telemetry.Capture(_entries.Count);
    }

    internal static string CreateKey(
        string snapshotFingerprint,
        string command,
        IReadOnlyList<string> arguments) {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, snapshotFingerprint);
        Append(hash, command);
        foreach (string argument in arguments) {
            Append(hash, argument);
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private void Trim() {
        while (_entries.Count > MaximumEntries && _insertionOrder.TryDequeue(out string? key)) {
            _entries.TryRemove(key, out _);
        }
    }

    private void PruneExpired() {
        DateTimeOffset now = timeProvider.GetUtcNow();
        foreach (KeyValuePair<string, CacheEntry> pair in _entries) {
            if (now - pair.Value.CreatedAtUtc > EntryLifetime) {
                _entries.TryRemove(pair.Key, out _);
            }
        }
    }

    private static void Append(IncrementalHash hash, string value) {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        hash.AppendData(BitConverter.GetBytes(bytes.Length));
        hash.AppendData(bytes);
    }

    private sealed record CacheEntry(WikiCommandResult Result, DateTimeOffset CreatedAtUtc);
}
