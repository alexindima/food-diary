using System.Globalization;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.Entities.Billing;

public sealed class BillingWebhookEvent : Entity<Guid> {
    public const string ReceivedStatus = "received";
    public const string ProcessedStatus = "processed";
    public const string FailedStatus = "failed";
    private const int ProviderMaxLength = 32;
    private const int EventIdMaxLength = 255;
    private const int EventTypeMaxLength = 128;
    private const int ExternalObjectIdMaxLength = 255;

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
            EventId = NormalizeRequired(eventId, EventIdMaxLength, nameof(eventId)),
            EventType = NormalizeRequired(eventType, EventTypeMaxLength, nameof(eventType)),
            ExternalObjectId = NormalizeOptional(externalObjectId, ExternalObjectIdMaxLength, nameof(externalObjectId)),
            Status = ReceivedStatus,
            ReceivedAtUtc = normalizedReceivedAtUtc,
            PayloadJson = DomainGuard.OptionalJson(payloadJson, nameof(payloadJson)),
            ParsedEventJson = DomainGuard.OptionalJson(parsedEventJson, nameof(parsedEventJson)),
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
            EventId = NormalizeRequired(eventId, EventIdMaxLength, nameof(eventId)),
            EventType = NormalizeRequired(eventType, EventTypeMaxLength, nameof(eventType)),
            ExternalObjectId = NormalizeOptional(externalObjectId, ExternalObjectIdMaxLength, nameof(externalObjectId)),
            Status = ProcessedStatus,
            ReceivedAtUtc = NormalizeRequiredUtc(processedAtUtc, nameof(processedAtUtc)),
            ProcessedAtUtc = NormalizeRequiredUtc(processedAtUtc, nameof(processedAtUtc)),
            PayloadJson = DomainGuard.OptionalJson(payloadJson, nameof(payloadJson)),
        };
        webhookEvent.SetCreated(webhookEvent.ReceivedAtUtc);
        return webhookEvent;
    }

    public void MarkProcessed(DateTime processedAtUtc) {
        if (string.Equals(Status, ProcessedStatus, StringComparison.Ordinal)) {
            return;
        }

        DateTime normalizedProcessedAt = NormalizeTransitionTimestamp(processedAtUtc, nameof(processedAtUtc));
        Status = ProcessedStatus;
        ProcessedAtUtc = normalizedProcessedAt;
        ErrorMessage = null;
        NextAttemptAtUtc = null;
        SetModified(ProcessedAtUtc.Value);
    }

    public void MarkFailed(DateTime failedAtUtc, string errorMessage) {
        if (string.Equals(Status, ProcessedStatus, StringComparison.Ordinal)) {
            throw new InvalidOperationException("A processed webhook event cannot be marked as failed.");
        }

        DateTime normalizedFailedAtUtc = NormalizeTransitionTimestamp(failedAtUtc, nameof(failedAtUtc));
        string normalizedError = NormalizeError(errorMessage);
        int nextAttemptCount = AttemptCount == int.MaxValue ? int.MaxValue : AttemptCount + 1;
        int delayMinutes = Math.Min(60, 1 << Math.Min(nextAttemptCount - 1, 6));
        DateTime nextAttemptAtUtc = AddMinutesSaturated(normalizedFailedAtUtc, delayMinutes);

        AttemptCount = nextAttemptCount;
        Status = FailedStatus;
        ErrorMessage = normalizedError;
        NextAttemptAtUtc = nextAttemptAtUtc;
        SetModified(normalizedFailedAtUtc);
    }

    private static string NormalizeProvider(string provider) {
        string normalized = NormalizeRequired(provider, ProviderMaxLength, nameof(provider));
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

    private static string NormalizeRequired(string value, int maxLength, string paramName) {
        string normalized = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", paramName)
            : value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
            : normalized;
    }

    private static string? NormalizeOptional(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeOptional(string? value, int maxLength, string paramName) {
        string? normalized = NormalizeOptional(value);
        return normalized?.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
            : normalized;
    }

    private static string NormalizeError(string errorMessage) {
        string normalized = string.IsNullOrWhiteSpace(errorMessage)
            ? throw new ArgumentException("Value is required.", nameof(errorMessage))
            : errorMessage.Trim();
        return normalized.Length <= 1024 ? normalized : normalized[..1024];
    }

    private static DateTime NormalizeRequiredUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.") : value.ToUniversalTime();
    }

    private DateTime NormalizeTransitionTimestamp(DateTime value, string paramName) {
        DateTime normalized = NormalizeRequiredUtc(value, paramName);
        DateTime earliestAllowed = ModifiedOnUtc ?? ReceivedAtUtc;
        return normalized < earliestAllowed
            ? throw new ArgumentOutOfRangeException(paramName, "Transition timestamp cannot be earlier than the previous webhook event timestamp.")
            : normalized;
    }

    private static DateTime AddMinutesSaturated(DateTime value, int minutes) {
        DateTime latestBase = DateTime.MaxValue.AddMinutes(-minutes);
        return value > latestBase
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : value.AddMinutes(minutes);
    }
}
