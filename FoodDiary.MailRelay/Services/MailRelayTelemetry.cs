using System.Diagnostics.Metrics;

namespace FoodDiary.MailRelay.Services;

public static class MailRelayTelemetry {
    public const string MeterName = "FoodDiary.MailRelay";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> QueueEventCounter = Meter.CreateCounter<long>(
        "fooddiary.mailrelay.queue.events");

    public static readonly Counter<long> OutboxEventCounter = Meter.CreateCounter<long>(
        "fooddiary.mailrelay.outbox.events");

    public static readonly Counter<long> InboxEventCounter = Meter.CreateCounter<long>(
        "fooddiary.mailrelay.inbox.events");

    public static readonly Counter<long> DeliveryEventCounter = Meter.CreateCounter<long>(
        "fooddiary.mailrelay.delivery.events");

    public static void RecordQueueEvent(string outcome) {
        QueueEventCounter.Add(1, new KeyValuePair<string, object?>("fooddiary.mailrelay.outcome", outcome));
    }

    public static void RecordOutboxEvent(string outcome) {
        OutboxEventCounter.Add(1, new KeyValuePair<string, object?>("fooddiary.mailrelay.outbox.outcome", outcome));
    }

    public static void RecordInboxEvent(string outcome) {
        InboxEventCounter.Add(1, new KeyValuePair<string, object?>("fooddiary.mailrelay.inbox.outcome", outcome));
    }

    public static void RecordDeliveryEvent(string outcome, string? errorType = null) {
        if (string.IsNullOrWhiteSpace(errorType)) {
            DeliveryEventCounter.Add(1, new KeyValuePair<string, object?>("fooddiary.mailrelay.delivery.outcome", outcome));
            return;
        }

        DeliveryEventCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.mailrelay.delivery.outcome", outcome),
            new KeyValuePair<string, object?>("error.type", errorType));
    }
}
