using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FoodDiary.Development.Mcp.Wiki;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ContextRoutingTelemetryStore(
    string filePath,
    TimeProvider? timeProvider = null,
    int maximumEvents = 1000) {
    private const int MinimumRetirementSamples = 100;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
    };
    private readonly string _filePath = Path.GetFullPath(
        string.IsNullOrWhiteSpace(filePath)
            ? throw new ArgumentException("A telemetry file path is required.", nameof(filePath))
            : filePath);
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly int _maximumEvents = maximumEvents > 0
        ? maximumEvents
        : throw new ArgumentOutOfRangeException(nameof(maximumEvents));
    private long _persistenceFailures;
    private DateTimeOffset? _lastPersistenceFailureAtUtc;

    public void Record(
        bool usedSqlite,
        string? fallbackReason,
        TimeSpan duration,
        bool refreshAttempted,
        bool refreshSucceeded) {
        try {
            WithExclusiveAccess(() => {
                List<ContextRoutingEvent> events = ReadEvents();
                events.Add(new ContextRoutingEvent(
                    _timeProvider.GetUtcNow(),
                    usedSqlite ? "sqlite-primary" : "json-fallback",
                    usedSqlite ? null : NormalizeFallbackReason(fallbackReason),
                    Math.Round(Math.Max(0, duration.TotalMilliseconds), 2, MidpointRounding.AwayFromZero),
                    refreshAttempted,
                    refreshSucceeded));
                if (events.Count > _maximumEvents) {
                    events.RemoveRange(0, events.Count - _maximumEvents);
                }
                WriteEvents(events);
            });
        } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            JsonException or TimeoutException) {
            Interlocked.Increment(ref _persistenceFailures);
            _lastPersistenceFailureAtUtc = _timeProvider.GetUtcNow();
        }
    }

    public ContextRoutingHealth Capture() {
        List<ContextRoutingEvent> events;
        try {
            events = WithExclusiveAccess(ReadEvents);
        } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            JsonException or TimeoutException) {
            Interlocked.Increment(ref _persistenceFailures);
            _lastPersistenceFailureAtUtc = _timeProvider.GetUtcNow();
            events = [];
        }

        int sqlitePrimary = events.Count(item => string.Equals(
            item.Route,
            "sqlite-primary",
            StringComparison.Ordinal));
        int jsonFallback = events.Count - sqlitePrimary;
        int noCandidateFallbacks = events.Count(item => string.Equals(
            item.FallbackReason,
            "sqlite-no-candidates",
            StringComparison.Ordinal));
        double[] durations = [.. events.Select(item => item.DurationMilliseconds).Order()];
        long persistenceFailures = Interlocked.Read(ref _persistenceFailures);
        var fallbackReasons = events
            .Where(item => item.FallbackReason is not null)
            .GroupBy(item => item.FallbackReason!, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        double fallbackRate = events.Count == 0
            ? 0
            : Math.Round((double)jsonFallback / events.Count, 4, MidpointRounding.AwayFromZero);

        return new ContextRoutingHealth(
            SampleCount: events.Count,
            SqlitePrimaryCount: sqlitePrimary,
            JsonFallbackCount: jsonFallback,
            JsonFallbackRate: fallbackRate,
            SqliteNoCandidateFallbackCount: noCandidateFallbacks,
            P50Milliseconds: Percentile(durations, 0.50),
            P95Milliseconds: Percentile(durations, 0.95),
            OldestSampleAtUtc: events.Count == 0 ? null : events[0].RecordedAtUtc,
            LatestSampleAtUtc: events.Count == 0 ? null : events[^1].RecordedAtUtc,
            FallbackReasonCounts: fallbackReasons,
            RetentionLimit: _maximumEvents,
            MinimumRetirementSamples,
            JsonFallbackRetirementReady: events.Count >= MinimumRetirementSamples &&
                fallbackRate <= 0.01 && persistenceFailures == 0,
            PersistenceHealthy: persistenceFailures == 0,
            PersistenceFailures: persistenceFailures,
            LastPersistenceFailureAtUtc: _lastPersistenceFailureAtUtc);
    }

    private static string NormalizeFallbackReason(string? fallbackReason) {
        if (string.IsNullOrWhiteSpace(fallbackReason)) {
            return "other";
        }
        if (fallbackReason.StartsWith("graph-refresh-", StringComparison.Ordinal)) {
            return "graph-refresh-failed";
        }
        if (fallbackReason.StartsWith("sqlite-error-", StringComparison.Ordinal)) {
            return "sqlite-error";
        }
        return fallbackReason switch {
            "database-missing" => fallbackReason,
            "fts-projection-not-ready" => fallbackReason,
            "snapshot-mismatch" => fallbackReason,
            "sqlite-no-candidates" => fallbackReason,
            "sqlite-reader-not-configured" => fallbackReason,
            _ => "other",
        };
    }

    private T WithExclusiveAccess<T>(Func<T> action) {
        using Mutex mutex = new(initiallyOwned: false, CreateMutexName());
        bool acquired;
        try {
            acquired = mutex.WaitOne(TimeSpan.FromSeconds(2));
        } catch (AbandonedMutexException) {
            acquired = true;
        }
        if (!acquired) {
            throw new TimeoutException("Timed out while persisting context routing telemetry.");
        }
        try {
            return action();
        } finally {
            mutex.ReleaseMutex();
        }
    }

    private void WithExclusiveAccess(Action action) => WithExclusiveAccess(() => {
        action();
        return true;
    });

    private List<ContextRoutingEvent> ReadEvents() {
        if (!File.Exists(_filePath)) {
            return [];
        }
        string json = File.ReadAllText(_filePath, Encoding.UTF8);
        ContextRoutingEnvelope? envelope = JsonSerializer.Deserialize<ContextRoutingEnvelope>(json, SerializerOptions);
        return envelope?.SchemaVersion == 1 ? envelope.Events ?? [] : [];
    }

    private void WriteEvents(IReadOnlyList<ContextRoutingEvent> events) {
        string? directory = Path.GetDirectoryName(_filePath);
        if (string.IsNullOrWhiteSpace(directory)) {
            throw new IOException("Telemetry path has no parent directory.");
        }
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(
            directory,
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $".{Path.GetFileName(_filePath)}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp"));
        try {
            string json = JsonSerializer.Serialize(
                new ContextRoutingEnvelope(1, [.. events]),
                SerializerOptions);
            using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough)) {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, _filePath, overwrite: true);
        } finally {
            if (File.Exists(temporaryPath)) {
                File.Delete(temporaryPath);
            }
        }
    }

    private string CreateMutexName() {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(_filePath.ToUpperInvariant()));
        return $"FoodDiary.LlmWiki.ContextRouting.{Convert.ToHexString(hash)}";
    }

    private static double Percentile(IReadOnlyList<double> samples, double percentile) {
        if (samples.Count == 0) {
            return 0;
        }
        int index = (int)Math.Ceiling(percentile * samples.Count) - 1;
        return Math.Round(samples[Math.Clamp(index, 0, samples.Count - 1)], 2, MidpointRounding.AwayFromZero);
    }

    private sealed record ContextRoutingEnvelope(int SchemaVersion, List<ContextRoutingEvent> Events);

    private sealed record ContextRoutingEvent(
        DateTimeOffset RecordedAtUtc,
        string Route,
        string? FallbackReason,
        double DurationMilliseconds,
        bool RefreshAttempted,
        bool RefreshSucceeded);
}
