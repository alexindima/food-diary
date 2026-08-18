using System.Globalization;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Billing;

public sealed class BillingPayment : Entity<Guid> {
    private const int ProviderMaxLength = 32;
    private const int ExternalIdMaxLength = 255;
    private const int PlanMaxLength = 32;
    private const int StatusMaxLength = 64;
    private const int KindMaxLength = 32;
    private const int CurrencyMaxLength = 3;

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

        if (billingSubscriptionId == Guid.Empty) {
            throw new ArgumentException("Billing subscription id cannot be empty.", nameof(billingSubscriptionId));
        }

        DateTime? normalizedPeriodStart = NormalizeOptionalUtc(currentPeriodStartUtc, nameof(currentPeriodStartUtc));
        DateTime? normalizedPeriodEnd = NormalizeOptionalUtc(currentPeriodEndUtc, nameof(currentPeriodEndUtc));
        EnsureChronologicalRange(normalizedPeriodStart, normalizedPeriodEnd, nameof(currentPeriodStartUtc));

        var payment = new BillingPayment {
            Id = Guid.NewGuid(),
            UserId = userId,
            BillingSubscriptionId = billingSubscriptionId,
            Provider = NormalizeProvider(provider),
            ExternalPaymentId = NormalizeRequired(externalPaymentId, ExternalIdMaxLength, nameof(externalPaymentId)),
            ExternalCustomerId = NormalizeOptional(externalCustomerId, ExternalIdMaxLength, nameof(externalCustomerId)),
            ExternalSubscriptionId = NormalizeOptional(externalSubscriptionId, ExternalIdMaxLength, nameof(externalSubscriptionId)),
            ExternalPaymentMethodId = NormalizeOptional(externalPaymentMethodId, ExternalIdMaxLength, nameof(externalPaymentMethodId)),
            ExternalPriceId = NormalizeOptional(externalPriceId, ExternalIdMaxLength, nameof(externalPriceId)),
            Plan = NormalizeOptional(plan, PlanMaxLength, nameof(plan)),
            Status = NormalizeRequired(status, StatusMaxLength, nameof(status)),
            Kind = NormalizeRequired(kind, KindMaxLength, nameof(kind)),
            Amount = amount,
            Currency = NormalizeOptional(currency, CurrencyMaxLength, nameof(currency)),
            Tax = tax,
            Fee = fee,
            Earnings = earnings,
            PayoutCurrency = NormalizeOptional(payoutCurrency, CurrencyMaxLength, nameof(payoutCurrency)),
            PayoutEarnings = payoutEarnings,
            OccurredAtUtc = NormalizeOptionalUtc(occurredAtUtc, nameof(occurredAtUtc)),
            CurrentPeriodStartUtc = normalizedPeriodStart,
            CurrentPeriodEndUtc = normalizedPeriodEnd,
            WebhookEventId = NormalizeOptional(webhookEventId, ExternalIdMaxLength, nameof(webhookEventId)),
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
        if (billingSubscriptionId == Guid.Empty) {
            throw new ArgumentException("Billing subscription id cannot be empty.", nameof(billingSubscriptionId));
        }

        Guid? normalizedBillingSubscriptionId = billingSubscriptionId ?? BillingSubscriptionId;
        string? normalizedCustomerId = NormalizeOptional(externalCustomerId, ExternalIdMaxLength, nameof(externalCustomerId)) ?? ExternalCustomerId;
        string? normalizedSubscriptionId = NormalizeOptional(externalSubscriptionId, ExternalIdMaxLength, nameof(externalSubscriptionId)) ?? ExternalSubscriptionId;
        string? normalizedPaymentMethodId = NormalizeOptional(externalPaymentMethodId, ExternalIdMaxLength, nameof(externalPaymentMethodId)) ?? ExternalPaymentMethodId;
        string? normalizedPriceId = NormalizeOptional(externalPriceId, ExternalIdMaxLength, nameof(externalPriceId)) ?? ExternalPriceId;
        string? normalizedPlan = NormalizeOptional(plan, PlanMaxLength, nameof(plan)) ?? Plan;
        string normalizedStatus = NormalizeRequired(status, StatusMaxLength, nameof(status));
        string normalizedKind = NormalizeRequired(kind, KindMaxLength, nameof(kind));
        string? normalizedCurrency = NormalizeOptional(currency, CurrencyMaxLength, nameof(currency)) ?? Currency;
        string? normalizedPayoutCurrency = NormalizeOptional(payoutCurrency, CurrencyMaxLength, nameof(payoutCurrency)) ?? PayoutCurrency;
        DateTime? normalizedOccurredAt = NormalizeOptionalUtc(occurredAtUtc, nameof(occurredAtUtc)) ?? OccurredAtUtc;
        DateTime? normalizedPeriodStart = NormalizeOptionalUtc(currentPeriodStartUtc, nameof(currentPeriodStartUtc)) ?? CurrentPeriodStartUtc;
        DateTime? normalizedPeriodEnd = NormalizeOptionalUtc(currentPeriodEndUtc, nameof(currentPeriodEndUtc)) ?? CurrentPeriodEndUtc;
        EnsureChronologicalRange(normalizedPeriodStart, normalizedPeriodEnd, nameof(currentPeriodStartUtc));
        string? normalizedWebhookEventId = NormalizeOptional(webhookEventId, ExternalIdMaxLength, nameof(webhookEventId)) ?? WebhookEventId;
        string? normalizedMetadata = NormalizeOptional(providerMetadataJson) ?? ProviderMetadataJson;

        BillingSubscriptionId = normalizedBillingSubscriptionId;
        ExternalCustomerId = normalizedCustomerId;
        ExternalSubscriptionId = normalizedSubscriptionId;
        ExternalPaymentMethodId = normalizedPaymentMethodId;
        ExternalPriceId = normalizedPriceId;
        Plan = normalizedPlan;
        Status = normalizedStatus;
        Kind = normalizedKind;
        Amount = amount ?? Amount;
        Currency = normalizedCurrency;
        Tax = tax ?? Tax;
        Fee = fee ?? Fee;
        Earnings = earnings ?? Earnings;
        PayoutCurrency = normalizedPayoutCurrency;
        PayoutEarnings = payoutEarnings ?? PayoutEarnings;
        OccurredAtUtc = normalizedOccurredAt;
        CurrentPeriodStartUtc = normalizedPeriodStart;
        CurrentPeriodEndUtc = normalizedPeriodEnd;
        WebhookEventId = normalizedWebhookEventId;
        ProviderMetadataJson = normalizedMetadata;
        SetModified();
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

    private static DateTime NormalizeRequiredUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.") : value.ToUniversalTime();
    }

    private static DateTime? NormalizeOptionalUtc(DateTime? value, string paramName) {
        return value.HasValue
            ? NormalizeRequiredUtc(value.Value, paramName)
            : null;
    }

    private static void EnsureChronologicalRange(DateTime? start, DateTime? end, string paramName) {
        if (start.HasValue && end.HasValue && start > end) {
            throw new ArgumentException("Start timestamp cannot be after end timestamp.", paramName);
        }
    }
}
