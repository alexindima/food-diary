using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace FoodDiary.Infrastructure.Services;

internal static class InfrastructureTelemetry {
    public const string MeterName = "FoodDiary.Infrastructure";

    private static readonly Meter Meter = new(MeterName);
    private static readonly ConcurrentDictionary<string, double> OutboxOldestPendingAgeSeconds = new(StringComparer.Ordinal);

    public static readonly Counter<long> AiRequestCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.requests");

    public static readonly Counter<long> AiQuotaRejectionCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.quota_rejections");

    public static readonly Counter<long> AiFallbackCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.fallbacks");

    public static readonly Counter<long> DatabaseCommandFailureCounter = Meter.CreateCounter<long>(
        "fooddiary.db.command.failures");

    public static readonly Counter<long> StorageOperationCounter = Meter.CreateCounter<long>(
        "fooddiary.storage.operations");

    public static readonly Counter<long> OutboxMessageCounter = Meter.CreateCounter<long>(
        "fooddiary.outbox.messages");

    public static readonly Histogram<double> OutboxProcessingDuration = Meter.CreateHistogram<double>(
        "fooddiary.outbox.processing.duration",
        unit: "ms");

    public static readonly ObservableGauge<double> OutboxOldestPendingAge = Meter.CreateObservableGauge(
        "fooddiary.outbox.pending.oldest_age",
        ObserveOutboxOldestPendingAge,
        unit: "s");

    internal static void RecordDatabaseCommandFailure(string operation, string source, string errorType) {
        DatabaseCommandFailureCounter.Add(
            1,
            new KeyValuePair<string, object?>("db.system", "postgresql"),
            new KeyValuePair<string, object?>("fooddiary.db.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.db.source", source),
            new KeyValuePair<string, object?>("error.type", errorType));
    }

    internal static void RecordOutboxMessages(string outboxName, string outcome, int count) {
        if (count <= 0) {
            return;
        }

        OutboxMessageCounter.Add(
            count,
            new KeyValuePair<string, object?>("fooddiary.outbox.name", outboxName),
            new KeyValuePair<string, object?>("fooddiary.outbox.outcome", outcome));
    }

    internal static void RecordOutboxProcessingDuration(string outboxName, double elapsedMilliseconds) {
        OutboxProcessingDuration.Record(
            elapsedMilliseconds,
            new KeyValuePair<string, object?>("fooddiary.outbox.name", outboxName));
    }

    internal static void RecordOutboxOldestPendingAge(string outboxName, DateTime nowUtc, DateTime? oldestCreatedOnUtc) {
        OutboxOldestPendingAgeSeconds[outboxName] = oldestCreatedOnUtc is null
            ? 0
            : Math.Max(0, (nowUtc - oldestCreatedOnUtc.Value).TotalSeconds);
    }

    private static IEnumerable<Measurement<double>> ObserveOutboxOldestPendingAge() =>
        OutboxOldestPendingAgeSeconds.Select(static item =>
            new Measurement<double>(
                item.Value,
                new KeyValuePair<string, object?>("fooddiary.outbox.name", item.Key)));

    internal static void RecordStorageOperation(string operation, string outcome, string? errorType = null) {
        StorageOperationCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.storage.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.storage.outcome", outcome),
            new KeyValuePair<string, object?>("error.type", errorType));
    }
}
