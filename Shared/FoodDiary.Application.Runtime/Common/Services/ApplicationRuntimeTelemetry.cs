using System.Diagnostics.Metrics;

namespace FoodDiary.Application.Runtime.Common.Services;

internal static class ApplicationRuntimeTelemetry {
    public const string MeterName = "FoodDiary.Application.Runtime";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> PostCommitActionCounter = Meter.CreateCounter<long>(
        "fooddiary.application.post_commit.actions");
    private static readonly Histogram<int> PostCommitQueueDepth = Meter.CreateHistogram<int>(
        "fooddiary.application.post_commit.queue_depth");
    private static readonly Histogram<double> PostCommitFlushDuration = Meter.CreateHistogram<double>(
        "fooddiary.application.post_commit.flush_duration",
        unit: "ms");

    public static void RecordQueueDepth(int depth) => PostCommitQueueDepth.Record(depth);

    public static void RecordActionSucceeded() => RecordAction("succeeded");

    public static void RecordActionFailed() => RecordAction("failed");

    public static void RecordActionTimedOut() => RecordAction("timed_out");

    public static void RecordActionDroppedByCapacity() => RecordAction("dropped", "capacity");

    public static void RecordActionsDroppedByFlushTimeout(int count) =>
        RecordAction("dropped", "flush_timeout", count);

    public static void RecordFlushDuration(TimeSpan duration) =>
        PostCommitFlushDuration.Record(duration.TotalMilliseconds);

    private static void RecordAction(string outcome, string? reason = null, int count = 1) {
        if (reason is null) {
            PostCommitActionCounter.Add(
                count,
                new KeyValuePair<string, object?>("fooddiary.post_commit.outcome", outcome));
            return;
        }

        PostCommitActionCounter.Add(
            count,
            new KeyValuePair<string, object?>("fooddiary.post_commit.outcome", outcome),
            new KeyValuePair<string, object?>("fooddiary.post_commit.reason", reason));
    }
}
