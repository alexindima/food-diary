using FoodDiary.Domain.Primitives;
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
    public decimal? Tax { get; private set; }
    public decimal? Fee { get; private set; }
    public decimal? Earnings { get; private set; }
    public string? PayoutCurrency { get; private set; }
    public decimal? PayoutEarnings { get; private set; }
    public DateTime? OccurredAtUtc { get; private set; }
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
        string? providerMetadataJson,
        decimal? tax = null,
        decimal? fee = null,
        decimal? earnings = null,
        string? payoutCurrency = null,
        decimal? payoutEarnings = null,
        DateTime? occurredAtUtc = null) {
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
            Tax = tax,
            Fee = fee,
            Earnings = earnings,
            PayoutCurrency = NormalizeOptional(payoutCurrency),
            PayoutEarnings = payoutEarnings,
            OccurredAtUtc = NormalizeOptionalUtc(occurredAtUtc, nameof(occurredAtUtc)),
            CurrentPeriodStartUtc = NormalizeOptionalUtc(currentPeriodStartUtc, nameof(currentPeriodStartUtc)),
            CurrentPeriodEndUtc = NormalizeOptionalUtc(currentPeriodEndUtc, nameof(currentPeriodEndUtc)),
            WebhookEventId = NormalizeOptional(webhookEventId),
            ProviderMetadataJson = NormalizeOptional(providerMetadataJson),
        };
        payment.SetCreated();
        return payment;
    }

    public void ApplyProviderResult(
        Guid? billingSubscriptionId,
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
        string? providerMetadataJson,
        decimal? tax = null,
        decimal? fee = null,
        decimal? earnings = null,
        string? payoutCurrency = null,
        decimal? payoutEarnings = null,
        DateTime? occurredAtUtc = null) {
        BillingSubscriptionId = billingSubscriptionId ?? BillingSubscriptionId;
        ExternalCustomerId = NormalizeOptional(externalCustomerId) ?? ExternalCustomerId;
        ExternalSubscriptionId = NormalizeOptional(externalSubscriptionId) ?? ExternalSubscriptionId;
        ExternalPaymentMethodId = NormalizeOptional(externalPaymentMethodId) ?? ExternalPaymentMethodId;
        ExternalPriceId = NormalizeOptional(externalPriceId) ?? ExternalPriceId;
        Plan = NormalizeOptional(plan) ?? Plan;
        Status = NormalizeRequired(status, nameof(status));
        Kind = NormalizeRequired(kind, nameof(kind));
        Amount = amount ?? Amount;
        Currency = NormalizeOptional(currency) ?? Currency;
        Tax = tax ?? Tax;
        Fee = fee ?? Fee;
        Earnings = earnings ?? Earnings;
        PayoutCurrency = NormalizeOptional(payoutCurrency) ?? PayoutCurrency;
        PayoutEarnings = payoutEarnings ?? PayoutEarnings;
        OccurredAtUtc = NormalizeOptionalUtc(occurredAtUtc, nameof(occurredAtUtc)) ?? OccurredAtUtc;
        CurrentPeriodStartUtc = NormalizeOptionalUtc(currentPeriodStartUtc, nameof(currentPeriodStartUtc)) ?? CurrentPeriodStartUtc;
        CurrentPeriodEndUtc = NormalizeOptionalUtc(currentPeriodEndUtc, nameof(currentPeriodEndUtc)) ?? CurrentPeriodEndUtc;
        WebhookEventId = NormalizeOptional(webhookEventId) ?? WebhookEventId;
        ProviderMetadataJson = NormalizeOptional(providerMetadataJson) ?? ProviderMetadataJson;
        SetModified();
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

    private static DateTime NormalizeRequiredUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.") : value.ToUniversalTime();
    }

    private static DateTime? NormalizeOptionalUtc(DateTime? value, string paramName) {
        return value.HasValue
            ? NormalizeRequiredUtc(value.Value, paramName)
            : null;
    }
}
