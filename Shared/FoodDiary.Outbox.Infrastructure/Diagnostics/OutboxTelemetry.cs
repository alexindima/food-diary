using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace FoodDiary.Outbox.Infrastructure.Diagnostics;

internal static class OutboxTelemetry {
    private static readonly Meter Meter = new("FoodDiary.Infrastructure");
    private static readonly ConcurrentDictionary<string, double> OutboxOldestPendingAgeSeconds = new(StringComparer.Ordinal);

    public static readonly Counter<long> OutboxMessageCounter = Meter.CreateCounter<long>(
        "fooddiary.outbox.messages");

    public static readonly Histogram<double> OutboxProcessingDuration = Meter.CreateHistogram<double>(
        "fooddiary.outbox.processing.duration",
        unit: "ms");

    public static readonly ObservableGauge<double> OutboxOldestPendingAge = Meter.CreateObservableGauge(
        "fooddiary.outbox.pending.oldest_age",
        ObserveOutboxOldestPendingAge,
        unit: "s");

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

}
