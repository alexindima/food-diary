using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Domain.Contracts;
using System.Text.Json;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookContextResolver(
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    ISender billingUserContextService,
    IBillingPaymentWriteRepository? billingPaymentRepository = null) {
    public async Task<string> GetSerializationKeyAsync(string provider, BillingWebhookEventModel webhookEvent, CancellationToken cancellationToken) {
        BillingSubscription? subscription = await ResolveSubscriptionAsync(provider, webhookEvent, cancellationToken).ConfigureAwait(false);
        BillingPayment? relatedPayment = await ResolveRelatedPaymentAsync(provider, webhookEvent.RelatedTransactionId, cancellationToken).ConfigureAwait(false);
        Guid? userId = subscription?.UserId.Value ?? (webhookEvent.UserId is { } metadataUserId && metadataUserId != Guid.Empty ? metadataUserId : relatedPayment?.UserId.Value);
        return userId is { } id && id != Guid.Empty
            ? BillingOperationLockKeys.ForUser(id)
            : $"billing-webhook:{provider.Trim().ToLowerInvariant()}:{webhookEvent.EventId}";
    }

    public async Task<Result<BillingWebhookProcessingContext?>> ResolveAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        BillingSubscription? subscription = await ResolveSubscriptionAsync(
            provider,
            webhookEvent,
            cancellationToken).ConfigureAwait(false);
        BillingPayment? payment = await ResolveRelatedPaymentAsync(provider,
            webhookEvent.ExternalPaymentId ?? webhookEvent.ExternalSubscriptionId, cancellationToken).ConfigureAwait(false);
        webhookEvent = NormalizeRenewal(provider, webhookEvent, subscription, payment);
        if (subscription is null && webhookEvent.UserId is { } ownerId && ownerId != Guid.Empty) {
            // A late notification from the previous provider still belongs to this user's payment history.
            subscription = await billingSubscriptionRepository.GetByUserIdAsync(new UserId(ownerId), cancellationToken).ConfigureAwait(false);
        }
        bool shouldUpdateSubscription = webhookEvent.UpdatesSubscription &&
            (subscription is null || string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase));
        if (webhookEvent.UpdatesSubscription &&
            subscription is not null &&
            string.Equals(subscription.LastWebhookEventId, webhookEvent.EventId, StringComparison.Ordinal)) {
            shouldUpdateSubscription = false;
        }

        if (webhookEvent.UpdatesSubscription &&
            subscription?.LastWebhookOccurredAtUtc is { } lastOccurredAtUtc &&
            (webhookEvent.OccurredAtUtc is not { } occurredAtUtc ||
             occurredAtUtc < lastOccurredAtUtc ||
             (occurredAtUtc == lastOccurredAtUtc && (!webhookEvent.IsAuthoritativeSnapshot ||
                 (webhookEvent.IsRenewal && !string.Equals(subscription.ExternalSubscriptionId,
                     webhookEvent.ExternalSubscriptionId, StringComparison.Ordinal)))))) {
            shouldUpdateSubscription = false;
        }

        if (webhookEvent.UpdatesSubscription && !shouldUpdateSubscription && !webhookEvent.Amount.HasValue) {
            return Result.Success<BillingWebhookProcessingContext?>(value: null);
        }

        BillingPayment? relatedPayment = await ResolveRelatedPaymentAsync(
            provider,
            webhookEvent.RelatedTransactionId,
            cancellationToken).ConfigureAwait(false);
        UserBillingProfileModel? user = await ResolveUserAsync(
            subscription,
            webhookEvent.UserId,
            relatedPayment?.UserId,
            cancellationToken).ConfigureAwait(false);
        return user is null
            ? Result.Failure<BillingWebhookProcessingContext?>(
                BillingErrors.WebhookValidationFailed("Webhook user could not be resolved."))
            : Result.Success<BillingWebhookProcessingContext?>(new BillingWebhookProcessingContext(subscription, user, shouldUpdateSubscription, webhookEvent));
    }

    private static BillingWebhookEventModel NormalizeRenewal(string provider, BillingWebhookEventModel webhookEvent,
        BillingSubscription? subscription, BillingPayment? payment) {
        if (!webhookEvent.IsRenewal && !string.Equals(payment?.Kind, BillingPaymentKinds.Renewal, StringComparison.Ordinal) &&
            !IsLegacyQueuedRenewal(provider, webhookEvent.ProviderMetadataJson)) {
            return webhookEvent;
        }
        if (!string.Equals(webhookEvent.Status, "active", StringComparison.Ordinal)) {
            // A declined attempt has an occurrence timestamp, but does not purchase a new subscription period.
            return webhookEvent with { IsRenewal = true, CurrentPeriodStartUtc = null, CurrentPeriodEndUtc = null };
        }

        // Older inbox payloads used the capture date as the period and had no typed renewal marker.
        DateTime? start = webhookEvent.IsRenewal ? webhookEvent.CurrentPeriodStartUtc : null;
        DateTime? end = webhookEvent.IsRenewal ? webhookEvent.CurrentPeriodEndUtc : null;
        if (payment is { Status: "active", CurrentPeriodStartUtc: not null, CurrentPeriodEndUtc: not null }) {
            start = payment.CurrentPeriodStartUtc;
            end = payment.CurrentPeriodEndUtc;
        } else if (subscription is { Status: "active" } &&
            string.Equals(subscription.ExternalSubscriptionId, webhookEvent.ExternalSubscriptionId, StringComparison.Ordinal)) {
            start = subscription.CurrentPeriodStartUtc;
            end = subscription.CurrentPeriodEndUtc;
        } else if (start is null) {
            // Compatibility with payments created before renewal_period_start metadata was introduced.
            bool currentPayment = subscription?.LastWebhookOccurredAtUtc is not { } lastOccurred ||
                webhookEvent.OccurredAtUtc > lastOccurred ||
                (webhookEvent.OccurredAtUtc == lastOccurred && string.Equals(subscription.ExternalSubscriptionId,
                    webhookEvent.ExternalSubscriptionId, StringComparison.Ordinal));
            start = payment?.CurrentPeriodEndUtc ?? (currentPayment ? subscription?.CurrentPeriodEndUtc : null) ?? webhookEvent.OccurredAtUtc;
            end = webhookEvent.Plan switch {
                "monthly" => start?.AddMonths(1),
                "yearly" => start?.AddYears(1),
                _ => null,
            };
        }
        return webhookEvent with { IsRenewal = true, CurrentPeriodStartUtc = start, CurrentPeriodEndUtc = end };
    }

    private static bool IsLegacyQueuedRenewal(string provider, string? metadataJson) {
        if (!string.Equals(provider, BillingProviderNames.YooKassa, StringComparison.OrdinalIgnoreCase) || metadataJson is null) {
            return false;
        }
        try {
            using var document = JsonDocument.Parse(metadataJson);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("metadata", out JsonElement metadata) && metadata.ValueKind == JsonValueKind.Object &&
                metadata.TryGetProperty("renewal", out JsonElement renewal) && renewal.ValueKind == JsonValueKind.String &&
                string.Equals(renewal.GetString(), "true", StringComparison.OrdinalIgnoreCase);
        } catch (JsonException) {
            return false;
        }
    }

    private async Task<BillingSubscription?> ResolveSubscriptionAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(webhookEvent.ExternalSubscriptionId)) {
            BillingSubscription? bySubscription = await billingSubscriptionRepository.GetByExternalSubscriptionIdAsync(
                provider,
                webhookEvent.ExternalSubscriptionId,
                cancellationToken).ConfigureAwait(false);
            if (bySubscription is not null) {
                return bySubscription;
            }
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.ExternalPaymentMethodId)) {
            return string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId)
                ? null
                : await billingSubscriptionRepository.GetByExternalCustomerIdAsync(
                    provider,
                    webhookEvent.ExternalCustomerId,
                    cancellationToken).ConfigureAwait(false);
        }

        BillingSubscription? byPaymentMethod = await billingSubscriptionRepository.GetByExternalPaymentMethodIdAsync(
            provider,
            webhookEvent.ExternalPaymentMethodId,
            cancellationToken).ConfigureAwait(false);
        return byPaymentMethod ?? (string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId)
            ? null
            : await billingSubscriptionRepository.GetByExternalCustomerIdAsync(
                provider,
                webhookEvent.ExternalCustomerId,
                cancellationToken).ConfigureAwait(false));
    }

    private Task<BillingPayment?> ResolveRelatedPaymentAsync(
        string provider,
        string? relatedTransactionId,
        CancellationToken cancellationToken) {
        return billingPaymentRepository is null || string.IsNullOrWhiteSpace(relatedTransactionId)
            ? Task.FromResult<BillingPayment?>(null)
            : billingPaymentRepository.GetByExternalPaymentIdAsync(provider, relatedTransactionId, cancellationToken);
    }

    private async Task<UserBillingProfileModel?> ResolveUserAsync(
        BillingSubscription? subscription,
        Guid? webhookUserId,
        UserId? relatedPaymentUserId,
        CancellationToken cancellationToken) {
        if (subscription is not null) {
            return await billingUserContextService.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: subscription.UserId), cancellationToken).ConfigureAwait(false);
        }

        if (webhookUserId.HasValue && webhookUserId.Value != Guid.Empty) {
            return await billingUserContextService.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: new UserId(webhookUserId.Value)), cancellationToken).ConfigureAwait(false);
        }

        return relatedPaymentUserId is null || relatedPaymentUserId == UserId.Empty
            ? null
            : await billingUserContextService.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: relatedPaymentUserId.Value), cancellationToken).ConfigureAwait(false);
    }
}
