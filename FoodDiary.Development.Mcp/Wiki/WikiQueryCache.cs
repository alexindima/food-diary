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
    private readonly Lock _gate = new();
    private readonly Dictionary<string, LinkedListNode<CacheEntry>> _entries = new(StringComparer.Ordinal);
    private readonly LinkedList<CacheEntry> _insertionOrder = new();

    public bool TryGet(
        string snapshotFingerprint,
        string command,
        IReadOnlyList<string> arguments,
        out WikiCommandResult? result,
        bool recordMetrics = true) {
        string key = CreateKey(snapshotFingerprint, command, arguments);
        bool hit = false;
        result = null;
        lock (_gate) {
            if (_entries.TryGetValue(key, out LinkedListNode<CacheEntry>? node)) {
                if (timeProvider.GetUtcNow() - node.Value.CreatedAtUtc <= EntryLifetime) {
                    result = node.Value.Result;
                    hit = true;
                } else {
                    Remove(node);
                }
            }
        }
        if (recordMetrics) {
            if (hit) { telemetry.RecordCacheHit(); } else { telemetry.RecordCacheMiss(); }
        }
        return hit;
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
        CacheEntry entry = new(key, result, timeProvider.GetUtcNow());
        lock (_gate) {
            if (_entries.TryGetValue(key, out LinkedListNode<CacheEntry>? node)) {
                node.Value = entry;
            } else {
                _entries.Add(key, _insertionOrder.AddLast(entry));
            }
            while (_entries.Count > MaximumEntries) {
                Remove(_insertionOrder.First!);
            }
        }
    }

    public WikiRuntimeMetrics CaptureMetrics() {
        int count;
        lock (_gate) {
            PruneExpired();
            count = _entries.Count;
        }
        return telemetry.Capture(count);
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

    private void Remove(LinkedListNode<CacheEntry> node) {
        _entries.Remove(node.Value.Key);
        _insertionOrder.Remove(node);
    }

    private void PruneExpired() {
        DateTimeOffset now = timeProvider.GetUtcNow();
        LinkedListNode<CacheEntry>? node = _insertionOrder.First;
        while (node is not null) {
            LinkedListNode<CacheEntry>? next = node.Next;
            if (now - node.Value.CreatedAtUtc > EntryLifetime) {
                Remove(node);
            }
            node = next;
        }
    }

    private static void Append(IncrementalHash hash, string value) {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        hash.AppendData(BitConverter.GetBytes(bytes.Length));
        hash.AppendData(bytes);
    }

    private sealed record CacheEntry(string Key, WikiCommandResult Result, DateTimeOffset CreatedAtUtc);
}
