namespace FoodDiary.MailRelay.Domain.DeliveryEvents;

public static class MailRelaySuppressionPolicy {
    public const string ComplaintReason = "complaint";
    public const string HardBounceReason = "hard-bounce";

    public static bool ShouldSuppress(string eventType, string? classification) {
        if (!MailRelayDeliveryEventType.TryNormalize(eventType, out var normalizedEventType)) {
            return false;
        }

        if (normalizedEventType == MailRelayDeliveryEventType.Complaint) {
            return true;
        }

        return normalizedEventType == MailRelayDeliveryEventType.Bounce &&
               string.Equals(classification, MailRelayBounceClassification.Hard, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetDefaultReason(string eventType) =>
        MailRelayDeliveryEventType.TryNormalize(eventType, out var normalizedEventType) &&
        normalizedEventType == MailRelayDeliveryEventType.Complaint
            ? ComplaintReason
            : HardBounceReason;
}
