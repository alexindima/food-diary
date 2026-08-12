using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookSubscriptionWriter(
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    TimeProvider dateTimeProvider) {
    public BillingWebhookEvent CreateProcessedEvent(
        string provider,
        BillingWebhookEventModel webhookEvent,
        string payload) =>
        BillingWebhookEvent.CreateProcessed(
            provider,
            webhookEvent.EventId,
            webhookEvent.EventType,
            webhookEvent.ExternalSubscriptionId ?? webhookEvent.ExternalPaymentMethodId,
            dateTimeProvider.GetUtcNow().UtcDateTime,
            payload);

    public async Task<BillingSubscription> UpsertAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        BillingSubscription? subscription,
        UserBillingProfileModel user,
        CancellationToken cancellationToken) {
        BillingSubscription currentSubscription = subscription ?? BillingSubscription.CreatePending(
            user.UserId,
            provider,
            webhookEvent.ExternalCustomerId,
            webhookEvent.ExternalPriceId,
            webhookEvent.Plan);

        currentSubscription.ApplyProviderSnapshot(
            provider,
            webhookEvent.ExternalSubscriptionId,
            webhookEvent.ExternalPaymentMethodId,
            webhookEvent.ExternalPriceId,
            webhookEvent.Plan,
            webhookEvent.Status,
            webhookEvent.CurrentPeriodStartUtc,
            webhookEvent.CurrentPeriodEndUtc,
            webhookEvent.CancelAtPeriodEnd,
            webhookEvent.CanceledAtUtc,
            webhookEvent.TrialStartUtc,
            webhookEvent.TrialEndUtc,
            webhookEvent.EventId,
            dateTimeProvider.GetUtcNow().UtcDateTime,
            webhookEvent.ProviderMetadataJson,
            webhookEvent.OccurredAtUtc);

        if (subscription is null) {
            await billingSubscriptionRepository.AddAsync(currentSubscription, cancellationToken).ConfigureAwait(false);
        } else {
            await billingSubscriptionRepository.UpdateAsync(currentSubscription, cancellationToken).ConfigureAwait(false);
        }

        return currentSubscription;
    }
}
