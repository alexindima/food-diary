namespace FoodDiary.MailRelay.Domain.DeliveryEvents;

public static class MailRelaySuppressionPolicy {
    public const string ComplaintReason = "complaint";
    public const string HardBounceReason = "hard-bounce";

    public static bool ShouldSuppress(string eventType, string? classification) {
        if (!MailRelayDeliveryEventType.TryNormalize(eventType, out string normalizedEventType)) {
            return false;
        }

        if (string.Equals(normalizedEventType, MailRelayDeliveryEventType.Complaint, StringComparison.Ordinal)) {
            return true;
        }

        return string.Equals(normalizedEventType, MailRelayDeliveryEventType.Bounce, StringComparison.Ordinal) &&
               string.Equals(classification, MailRelayBounceClassification.Hard, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetDefaultReason(string eventType) =>
        MailRelayDeliveryEventType.TryNormalize(eventType, out string normalizedEventType) &&
        string.Equals(normalizedEventType, MailRelayDeliveryEventType.Complaint, StringComparison.Ordinal)
            ? ComplaintReason
            : HardBounceReason;
}
