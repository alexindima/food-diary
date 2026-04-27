using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Billing;

public sealed class BillingPayment : Entity<Guid> {
    public UserId UserId { get; private set; }
    public Guid? BillingSubscriptionId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalPaymentId { get; private set; } = string.Empty;
    public string? ExternalCustomerId { get; private set; }
    public string? ExternalSubscriptionId { get; private set; }
    public string? ExternalPaymentMethodId { get; private set; }
    public string? ExternalPriceId { get; private set; }
    public string? Plan { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string Kind { get; private set; } = string.Empty;
    public decimal? Amount { get; private set; }
    public string? Currency { get; private set; }
    public DateTime? CurrentPeriodStartUtc { get; private set; }
    public DateTime? CurrentPeriodEndUtc { get; private set; }
    public string? WebhookEventId { get; private set; }
    public string? ProviderMetadataJson { get; private set; }

    private BillingPayment() {
    }

    public static BillingPayment Create(
        UserId userId,
        Guid? billingSubscriptionId,
        string provider,
        string externalPaymentId,
        string? externalCustomerId,
        string? externalSubscriptionId,
        string? externalPaymentMethodId,
        string? externalPriceId,
        string? plan,
        string status,
        string kind,
        decimal? amount,
        string? currency,
        DateTime? currentPeriodStartUtc,
        DateTime? currentPeriodEndUtc,
        string? webhookEventId,
        string? providerMetadataJson) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var payment = new BillingPayment {
            Id = Guid.NewGuid(),
            UserId = userId,
            BillingSubscriptionId = billingSubscriptionId,
            Provider = NormalizeProvider(provider),
            ExternalPaymentId = NormalizeRequired(externalPaymentId, nameof(externalPaymentId)),
            ExternalCustomerId = NormalizeOptional(externalCustomerId),
            ExternalSubscriptionId = NormalizeOptional(externalSubscriptionId),
            ExternalPaymentMethodId = NormalizeOptional(externalPaymentMethodId),
            ExternalPriceId = NormalizeOptional(externalPriceId),
            Plan = NormalizeOptional(plan),
            Status = NormalizeRequired(status, nameof(status)),
            Kind = NormalizeRequired(kind, nameof(kind)),
            Amount = amount,
            Currency = NormalizeOptional(currency),
            CurrentPeriodStartUtc = NormalizeOptionalUtc(currentPeriodStartUtc, nameof(currentPeriodStartUtc)),
            CurrentPeriodEndUtc = NormalizeOptionalUtc(currentPeriodEndUtc, nameof(currentPeriodEndUtc)),
            WebhookEventId = NormalizeOptional(webhookEventId),
            ProviderMetadataJson = NormalizeOptional(providerMetadataJson),
        };
        payment.SetCreated();
        return payment;
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

    private static DateTime? NormalizeOptionalUtc(DateTime? value, string paramName) {
        return value.HasValue
            ? NormalizeRequiredUtc(value.Value, paramName)
            : null;
    }
}
