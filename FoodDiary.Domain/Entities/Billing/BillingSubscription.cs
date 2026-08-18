using System.Globalization;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Billing;

public sealed class BillingSubscription : Entity<Guid> {
    public const string PendingCheckoutStatus = "pending_checkout";
    private const int ProviderMaxLength = 32;
    private const int ExternalIdMaxLength = 255;
    private const int PlanMaxLength = 32;
    private const int StatusMaxLength = 64;

    public UserId UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalCustomerId { get; private set; } = string.Empty;
    public string? ExternalSubscriptionId { get; private set; }
    public string? ExternalPaymentMethodId { get; private set; }
    public string? ExternalPriceId { get; private set; }
    public string? Plan { get; private set; }
    public string Status { get; private set; } = PendingCheckoutStatus;
    public DateTime? CurrentPeriodStartUtc { get; private set; }
    public DateTime? CurrentPeriodEndUtc { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTime? CanceledAtUtc { get; private set; }
    public DateTime? TrialStartUtc { get; private set; }
    public DateTime? TrialEndUtc { get; private set; }
    public DateTime? NextBillingAttemptUtc { get; private set; }
    public string? ProviderMetadataJson { get; private set; }
    public string? LastWebhookEventId { get; private set; }
    public DateTime? LastWebhookOccurredAtUtc { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }
    public bool PremiumRoleManagedByBilling { get; private set; }

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
            ExternalCustomerId = NormalizeRequired(externalCustomerId, ExternalIdMaxLength, nameof(externalCustomerId)),
            ExternalPriceId = NormalizeOptional(externalPriceId, ExternalIdMaxLength, nameof(externalPriceId)),
            Plan = NormalizeOptional(plan, PlanMaxLength, nameof(plan)),
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
        string normalizedProvider = NormalizeProvider(provider);
        string normalizedCustomerId = NormalizeRequired(externalCustomerId, ExternalIdMaxLength, nameof(externalCustomerId));
        string? normalizedPriceId = NormalizeOptional(externalPriceId, ExternalIdMaxLength, nameof(externalPriceId));
        string? normalizedPlan = NormalizeOptional(plan, PlanMaxLength, nameof(plan));

        Provider = normalizedProvider;
        ExternalCustomerId = normalizedCustomerId;
        ExternalPriceId = normalizedPriceId;
        Plan = normalizedPlan;
        Status = PendingCheckoutStatus;
        SetModified();
    }

    public void ApplyProviderSnapshot(
        string provider,
        string? externalSubscriptionId,
        string? externalPaymentMethodId,
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
        DateTime syncedAtUtc,
        string? providerMetadataJson = null,
        DateTime? webhookOccurredAtUtc = null) {
        string normalizedProvider = NormalizeProvider(provider);
        string? normalizedSubscriptionId = NormalizeOptional(externalSubscriptionId, ExternalIdMaxLength, nameof(externalSubscriptionId));
        string? normalizedPaymentMethodId = NormalizeOptional(externalPaymentMethodId, ExternalIdMaxLength, nameof(externalPaymentMethodId));
        string? normalizedPriceId = NormalizeOptional(externalPriceId, ExternalIdMaxLength, nameof(externalPriceId));
        string? normalizedPlan = NormalizeOptional(plan, PlanMaxLength, nameof(plan));
        string normalizedStatus = NormalizeRequired(status, StatusMaxLength, nameof(status));
        DateTime? normalizedPeriodStart = NormalizeOptionalUtc(currentPeriodStartUtc, nameof(currentPeriodStartUtc));
        DateTime? normalizedPeriodEnd = NormalizeOptionalUtc(currentPeriodEndUtc, nameof(currentPeriodEndUtc));
        EnsureChronologicalRange(normalizedPeriodStart, normalizedPeriodEnd, nameof(currentPeriodStartUtc));
        DateTime? normalizedCanceledAt = NormalizeOptionalUtc(canceledAtUtc, nameof(canceledAtUtc));
        DateTime? normalizedTrialStart = NormalizeOptionalUtc(trialStartUtc, nameof(trialStartUtc));
        DateTime? normalizedTrialEnd = NormalizeOptionalUtc(trialEndUtc, nameof(trialEndUtc));
        EnsureChronologicalRange(normalizedTrialStart, normalizedTrialEnd, nameof(trialStartUtc));
        string? normalizedMetadata = NormalizeOptional(providerMetadataJson);
        string normalizedWebhookEventId = NormalizeRequired(webhookEventId, ExternalIdMaxLength, nameof(webhookEventId));
        DateTime? normalizedWebhookOccurredAt = NormalizeOptionalUtc(webhookOccurredAtUtc, nameof(webhookOccurredAtUtc));
        DateTime normalizedSyncedAt = NormalizeRequiredUtc(syncedAtUtc, nameof(syncedAtUtc));

        Provider = normalizedProvider;
        ExternalSubscriptionId = normalizedSubscriptionId;
        ExternalPaymentMethodId = normalizedPaymentMethodId;
        ExternalPriceId = normalizedPriceId;
        Plan = normalizedPlan;
        Status = normalizedStatus;
        CurrentPeriodStartUtc = normalizedPeriodStart;
        CurrentPeriodEndUtc = normalizedPeriodEnd;
        CancelAtPeriodEnd = cancelAtPeriodEnd;
        CanceledAtUtc = normalizedCanceledAt;
        TrialStartUtc = normalizedTrialStart;
        TrialEndUtc = normalizedTrialEnd;
        NextBillingAttemptUtc = ResolveNextBillingAttemptUtc(Status, CancelAtPeriodEnd, CurrentPeriodEndUtc);
        ProviderMetadataJson = normalizedMetadata;
        LastWebhookEventId = normalizedWebhookEventId;
        LastWebhookOccurredAtUtc = normalizedWebhookOccurredAt;
        LastSyncedAtUtc = normalizedSyncedAt;
        SetModified(LastSyncedAtUtc.Value);
    }

    public void MarkPremiumRoleManagedByBilling(bool value, DateTime changedAtUtc) {
        DateTime normalizedChangedAt = NormalizeRequiredUtc(changedAtUtc, nameof(changedAtUtc));
        if (PremiumRoleManagedByBilling == value) {
            return;
        }

        PremiumRoleManagedByBilling = value;
        SetModified(normalizedChangedAt);
    }

    public void MarkRenewalFailed(
        DateTime nextBillingAttemptUtc,
        string eventId,
        DateTime syncedAtUtc,
        string? providerMetadataJson = null) {
        DateTime normalizedNextBillingAttempt = NormalizeRequiredUtc(nextBillingAttemptUtc, nameof(nextBillingAttemptUtc));
        string normalizedEventId = NormalizeRequired(eventId, ExternalIdMaxLength, nameof(eventId));
        DateTime normalizedSyncedAt = NormalizeRequiredUtc(syncedAtUtc, nameof(syncedAtUtc));
        string? normalizedMetadata = NormalizeOptional(providerMetadataJson);

        Status = "past_due";
        NextBillingAttemptUtc = normalizedNextBillingAttempt;
        LastWebhookEventId = normalizedEventId;
        LastSyncedAtUtc = normalizedSyncedAt;
        ProviderMetadataJson = normalizedMetadata;
        SetModified(LastSyncedAtUtc.Value);
    }

    public void MarkRenewalSkippedForInaccessibleUser(
        string eventId,
        DateTime syncedAtUtc,
        string? providerMetadataJson = null) {
        DateTime normalizedSyncedAt = NormalizeRequiredUtc(syncedAtUtc, nameof(syncedAtUtc));
        string normalizedEventId = NormalizeRequired(eventId, ExternalIdMaxLength, nameof(eventId));
        string? normalizedMetadata = NormalizeOptional(providerMetadataJson);

        Status = "canceled";
        CancelAtPeriodEnd = false;
        CanceledAtUtc = normalizedSyncedAt;
        NextBillingAttemptUtc = null;
        LastWebhookEventId = normalizedEventId;
        LastSyncedAtUtc = CanceledAtUtc;
        ProviderMetadataJson = normalizedMetadata;
        SetModified(LastSyncedAtUtc.Value);
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
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value, int maxLength, string paramName) {
        string? normalized = NormalizeOptional(value);
        return normalized?.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
            : normalized;
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

    private static DateTime? ResolveNextBillingAttemptUtc(
        string status,
        bool cancelAtPeriodEnd,
        DateTime? currentPeriodEndUtc) {
        if (cancelAtPeriodEnd || !currentPeriodEndUtc.HasValue) {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch {
            "active" or "trialing" or "past_due" => currentPeriodEndUtc,
            _ => null,
        };
    }

    private static void EnsureChronologicalRange(DateTime? start, DateTime? end, string paramName) {
        if (start.HasValue && end.HasValue && start > end) {
            throw new ArgumentException("Start timestamp cannot be after end timestamp.", paramName);
        }
    }
}
