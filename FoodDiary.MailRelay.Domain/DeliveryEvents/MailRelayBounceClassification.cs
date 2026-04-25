namespace FoodDiary.MailRelay.Domain.DeliveryEvents;

public static class MailRelayBounceClassification {
    public const string Hard = "hard";
    public const string Soft = "soft";

    public static bool IsSupportedOptional(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return true;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is Hard or Soft;
    }
}
