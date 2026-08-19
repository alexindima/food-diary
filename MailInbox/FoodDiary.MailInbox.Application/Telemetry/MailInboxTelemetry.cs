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

    public static readonly Counter<long> AdmissionCounter = Meter.CreateCounter<long>(
        "fooddiary.mailinbox.admission.events");

    public static readonly Counter<long> RetentionCounter = Meter.CreateCounter<long>(
        "fooddiary.mailinbox.retention.events");

    public static void RecordIngestion(
        MailInboxIngestionOutcome outcome,
        TimeSpan duration,
        long messageSizeBytes) {
        KeyValuePair<string, object?> outcomeTag = new("fooddiary.mailinbox.outcome", ToTagValue(outcome));
        IngestionCounter.Add(1, outcomeTag);
        IngestionDuration.Record(duration.TotalMilliseconds, outcomeTag);
        MessageSize.Record(messageSizeBytes, outcomeTag);
    }

    public static void RecordAdmission(MailInboxAdmissionOutcome outcome) =>
        AdmissionCounter.Add(1, new KeyValuePair<string, object?>("fooddiary.mailinbox.outcome", ToTagValue(outcome)));

    public static void RecordRetention(MailInboxRetentionOutcome outcome, int count) =>
        RetentionCounter.Add(count, new KeyValuePair<string, object?>("fooddiary.mailinbox.outcome", ToTagValue(outcome)));

    private static string ToTagValue(MailInboxIngestionOutcome outcome) => outcome switch {
        MailInboxIngestionOutcome.Overloaded => "overloaded",
        MailInboxIngestionOutcome.EmptyMessage => "empty_message",
        MailInboxIngestionOutcome.MessageTooLarge => "message_too_large",
        MailInboxIngestionOutcome.IpByteRateLimited => "ip_byte_rate_limited",
        MailInboxIngestionOutcome.MimePartLimit => "mime_part_limit",
        MailInboxIngestionOutcome.RecipientLimit => "recipient_limit",
        MailInboxIngestionOutcome.MetadataLimit => "metadata_limit",
        MailInboxIngestionOutcome.Duplicate => "duplicate",
        MailInboxIngestionOutcome.Success => "success",
        MailInboxIngestionOutcome.Canceled => "canceled",
        MailInboxIngestionOutcome.StorageQuota => "storage_quota",
        MailInboxIngestionOutcome.Failure => "failure",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, message: null),
    };

    private static string ToTagValue(MailInboxAdmissionOutcome outcome) => outcome switch {
        MailInboxAdmissionOutcome.MessageTooLarge => "message_too_large",
        MailInboxAdmissionOutcome.SessionRateLimited => "session_rate_limited",
        MailInboxAdmissionOutcome.IpRateLimited => "ip_rate_limited",
        MailInboxAdmissionOutcome.SenderRateLimited => "sender_rate_limited",
        MailInboxAdmissionOutcome.Accepted => "accepted",
        MailInboxAdmissionOutcome.RecipientNotAllowed => "recipient_not_allowed",
        MailInboxAdmissionOutcome.RecipientLimitExceeded => "recipient_limit_exceeded",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, message: null),
    };

    private static string ToTagValue(MailInboxRetentionOutcome outcome) => outcome switch {
        MailInboxRetentionOutcome.Failure => "failure",
        MailInboxRetentionOutcome.ContentPurged => "content_purged",
        MailInboxRetentionOutcome.MetadataDeleted => "metadata_deleted",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, message: null),
    };
}
