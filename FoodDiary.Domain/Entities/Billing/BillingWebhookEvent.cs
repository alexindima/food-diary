using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.Billing;

public sealed class BillingWebhookEvent : Entity<Guid> {
    public string Provider { get; private set; } = string.Empty;
    public string EventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string? ExternalObjectId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; private set; }
    public string? PayloadJson { get; private set; }
    public string? ErrorMessage { get; private set; }

    private BillingWebhookEvent() {
    }

    public static BillingWebhookEvent CreateProcessed(
        string provider,
        string eventId,
        string eventType,
        string? externalObjectId,
        DateTime processedAtUtc,
        string? payloadJson) {
        var webhookEvent = new BillingWebhookEvent {
            Id = Guid.NewGuid(),
            Provider = NormalizeProvider(provider),
            EventId = NormalizeRequired(eventId, nameof(eventId)),
            EventType = NormalizeRequired(eventType, nameof(eventType)),
            ExternalObjectId = NormalizeOptional(externalObjectId),
            Status = "processed",
            ProcessedAtUtc = NormalizeRequiredUtc(processedAtUtc, nameof(processedAtUtc)),
            PayloadJson = NormalizeOptional(payloadJson),
        };
        webhookEvent.SetCreated(webhookEvent.ProcessedAtUtc);
        return webhookEvent;
    }

    private static string NormalizeProvider(string provider) {
        var normalized = NormalizeRequired(provider, nameof(provider));
        if (!BillingProviderNames.IsSupported(normalized)) {
            throw new ArgumentException("Unsupported billing provider.", nameof(provider));
        }

        if (string.Equals(normalized, BillingProviderNames.Paddle, StringComparison.OrdinalIgnoreCase)) {
            return BillingProviderNames.Paddle;
        }

        if (string.Equals(normalized, BillingProviderNames.YooKassa, StringComparison.OrdinalIgnoreCase)) {
            return BillingProviderNames.YooKassa;
        }

        return BillingProviderNames.Stripe;
    }

    private static string NormalizeRequired(string value, string paramName) {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", paramName)
            : value.Trim();
    }

    private static string? NormalizeOptional(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value.Trim();
    }

    private static DateTime NormalizeRequiredUtc(DateTime value, string paramName) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
    }
}
