using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.Entities.Billing;

public sealed class BillingWebhookEvent : Entity<Guid> {
    public const string ReceivedStatus = "received";
    public const string ProcessedStatus = "processed";
    public const string FailedStatus = "failed";

    public string Provider { get; private set; } = string.Empty;
    public string EventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string? ExternalObjectId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }
    public string? PayloadJson { get; private set; }
    public string? ParsedEventJson { get; private set; }
    public string? ErrorMessage { get; private set; }

    private BillingWebhookEvent() {
    }

    public static BillingWebhookEvent CreateReceived(
        string provider,
        string eventId,
        string eventType,
        string? externalObjectId,
        DateTime receivedAtUtc,
        string payloadJson,
        string parsedEventJson) {
        DateTime normalizedReceivedAtUtc = NormalizeRequiredUtc(receivedAtUtc, nameof(receivedAtUtc));
        var webhookEvent = new BillingWebhookEvent {
            Id = Guid.NewGuid(),
            Provider = NormalizeProvider(provider),
            EventId = NormalizeRequired(eventId, nameof(eventId)),
            EventType = NormalizeRequired(eventType, nameof(eventType)),
            ExternalObjectId = NormalizeOptional(externalObjectId),
            Status = ReceivedStatus,
            ReceivedAtUtc = normalizedReceivedAtUtc,
            PayloadJson = NormalizeOptional(payloadJson),
            ParsedEventJson = NormalizeOptional(parsedEventJson),
        };
        webhookEvent.SetCreated(normalizedReceivedAtUtc);
        return webhookEvent;
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
            Status = ProcessedStatus,
            ReceivedAtUtc = NormalizeRequiredUtc(processedAtUtc, nameof(processedAtUtc)),
            ProcessedAtUtc = NormalizeRequiredUtc(processedAtUtc, nameof(processedAtUtc)),
            PayloadJson = NormalizeOptional(payloadJson),
        };
        webhookEvent.SetCreated(webhookEvent.ReceivedAtUtc);
        return webhookEvent;
    }

    public void MarkProcessed(DateTime processedAtUtc) {
        Status = ProcessedStatus;
        ProcessedAtUtc = NormalizeRequiredUtc(processedAtUtc, nameof(processedAtUtc));
        ErrorMessage = null;
        NextAttemptAtUtc = null;
        SetModified(ProcessedAtUtc.Value);
    }

    public void MarkFailed(DateTime failedAtUtc, string errorMessage) {
        DateTime normalizedFailedAtUtc = NormalizeRequiredUtc(failedAtUtc, nameof(failedAtUtc));
        AttemptCount++;
        Status = FailedStatus;
        ErrorMessage = NormalizeError(errorMessage);
        int delayMinutes = Math.Min(60, 1 << Math.Min(AttemptCount - 1, 6));
        NextAttemptAtUtc = normalizedFailedAtUtc.AddMinutes(delayMinutes);
        SetModified(normalizedFailedAtUtc);
    }

    private static string NormalizeProvider(string provider) {
        string normalized = NormalizeRequired(provider, nameof(provider));
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
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeError(string errorMessage) {
        string normalized = NormalizeRequired(errorMessage, nameof(errorMessage));
        return normalized.Length <= 1024 ? normalized : normalized[..1024];
    }

    private static DateTime NormalizeRequiredUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.") : value.ToUniversalTime();
    }
}
