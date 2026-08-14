using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FoodDiary.MailInbox.Application.Telemetry;

public static class MailInboxTelemetry {
    public const string MeterName = "FoodDiary.MailInbox";

    private static readonly Meter Meter = new(MeterName);

    public static readonly ActivitySource ActivitySource = new(MeterName);

    public static readonly Counter<long> IngestionCounter = Meter.CreateCounter<long>(
        "fooddiary.mailinbox.ingestion.events");

    public static readonly Histogram<double> IngestionDuration = Meter.CreateHistogram<double>(
        "fooddiary.mailinbox.ingestion.duration_ms");

    public static readonly Histogram<long> MessageSize = Meter.CreateHistogram<long>(
        "fooddiary.mailinbox.message.size_bytes");

    public static void RecordIngestion(string outcome, TimeSpan duration, long messageSizeBytes) {
        KeyValuePair<string, object?> outcomeTag = new("fooddiary.mailinbox.outcome", outcome);
        IngestionCounter.Add(1, outcomeTag);
        IngestionDuration.Record(duration.TotalMilliseconds, outcomeTag);
        MessageSize.Record(messageSizeBytes, outcomeTag);
    }
}
