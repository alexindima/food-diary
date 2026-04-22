using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Billing;

public sealed class BillingSubscription : Entity<Guid> {
    public const string PendingCheckoutStatus = "pending_checkout";

    public UserId UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalCustomerId { get; private set; } = string.Empty;
    public string? ExternalSubscriptionId { get; private set; }
    public string? ExternalPriceId { get; private set; }
    public string? Plan { get; private set; }
    public string Status { get; private set; } = PendingCheckoutStatus;
    public DateTime? CurrentPeriodStartUtc { get; private set; }
    public DateTime? CurrentPeriodEndUtc { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTime? CanceledAtUtc { get; private set; }
    public DateTime? TrialStartUtc { get; private set; }
    public DateTime? TrialEndUtc { get; private set; }
    public string? LastWebhookEventId { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }

    private BillingSubscription() {
    }

    public static BillingSubscription CreatePending(
        UserId userId,
        string provider,
        string externalCustomerId,
        string? externalPriceId,
        string? plan) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var subscription = new BillingSubscription {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = NormalizeProvider(provider),
            ExternalCustomerId = NormalizeRequired(externalCustomerId, nameof(externalCustomerId)),
            ExternalPriceId = NormalizeOptional(externalPriceId),
            Plan = NormalizeOptional(plan),
            Status = PendingCheckoutStatus,
        };
        subscription.SetCreated();
        return subscription;
    }

    public void UpdateCheckoutContext(
        string provider,
        string externalCustomerId,
        string? externalPriceId,
        string? plan) {
        Provider = NormalizeProvider(provider);
        ExternalCustomerId = NormalizeRequired(externalCustomerId, nameof(externalCustomerId));
        ExternalPriceId = NormalizeOptional(externalPriceId);
        Plan = NormalizeOptional(plan);
        SetModified();
    }

    public void ApplyProviderSnapshot(
        string provider,
        string? externalSubscriptionId,
        string? externalPriceId,
        string? plan,
        string status,
        DateTime? currentPeriodStartUtc,
        DateTime? currentPeriodEndUtc,
        bool cancelAtPeriodEnd,
        DateTime? canceledAtUtc,
        DateTime? trialStartUtc,
        DateTime? trialEndUtc,
        string webhookEventId,
        DateTime syncedAtUtc) {
        Provider = NormalizeProvider(provider);
        ExternalSubscriptionId = NormalizeOptional(externalSubscriptionId);
        ExternalPriceId = NormalizeOptional(externalPriceId);
        Plan = NormalizeOptional(plan);
        Status = NormalizeRequired(status, nameof(status));
        CurrentPeriodStartUtc = NormalizeOptionalUtc(currentPeriodStartUtc, nameof(currentPeriodStartUtc));
        CurrentPeriodEndUtc = NormalizeOptionalUtc(currentPeriodEndUtc, nameof(currentPeriodEndUtc));
        CancelAtPeriodEnd = cancelAtPeriodEnd;
        CanceledAtUtc = NormalizeOptionalUtc(canceledAtUtc, nameof(canceledAtUtc));
        TrialStartUtc = NormalizeOptionalUtc(trialStartUtc, nameof(trialStartUtc));
        TrialEndUtc = NormalizeOptionalUtc(trialEndUtc, nameof(trialEndUtc));
        LastWebhookEventId = NormalizeRequired(webhookEventId, nameof(webhookEventId));
        LastSyncedAtUtc = NormalizeRequiredUtc(syncedAtUtc, nameof(syncedAtUtc));
        SetModified(LastSyncedAtUtc.Value);
    }

    private static string NormalizeProvider(string provider) {
        var normalized = NormalizeRequired(provider, nameof(provider));
        if (!BillingProviderNames.IsSupported(normalized)) {
            throw new ArgumentException("Unsupported billing provider.", nameof(provider));
        }

        return string.Equals(normalized, BillingProviderNames.Paddle, StringComparison.OrdinalIgnoreCase)
            ? BillingProviderNames.Paddle
            : BillingProviderNames.Stripe;
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
