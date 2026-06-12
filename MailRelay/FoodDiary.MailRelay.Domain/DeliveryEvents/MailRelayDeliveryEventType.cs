namespace FoodDiary.MailRelay.Domain.DeliveryEvents;

public static class MailRelayDeliveryEventType {
    public const string Bounce = "bounce";
    public const string Complaint = "complaint";

    public static bool TryNormalize(string? value, out string normalized) {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        normalized = value.Trim().ToLowerInvariant();
        if (normalized is Bounce or Complaint) {
            return true;
        }

        normalized = string.Empty;
        return false;
    }

    public static bool IsSupported(string? value) => TryNormalize(value, out _);
}
