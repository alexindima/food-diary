using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Services;

public sealed class BillingCancellationRecorder(IBillingCancellationRepository cancellations, TimeProvider timeProvider) {
    public async Task<BillingCancellation?> SynchronizeAsync(BillingSubscription subscription, string source, CancellationToken cancellationToken) {
        if (!string.Equals(subscription.Provider, BillingProviderNames.Paddle, StringComparison.Ordinal) || subscription.ExternalSubscriptionId is not { } externalId) {
            return null;
        }
        BillingCancellation? latest = await cancellations.GetLatestAsync(subscription.UserId, subscription.Provider, externalId, cancellationToken);
        bool canceled = subscription.Status is "canceled";
        if (!canceled && !subscription.CancelAtPeriodEnd) {
            if (latest is { Outcome: "scheduled" } && subscription.Status is "active" or "trialing" or "past_due" or "paused") {
                latest.Synchronize(subscription, timeProvider.GetUtcNow().UtcDateTime);
                await cancellations.UpdateAsync(latest, cancellationToken);
            }
            return null;
        }
        if (latest is null || latest.Outcome is "cancellation_removed") {
            DateTime now = timeProvider.GetUtcNow().UtcDateTime;
            DateTime effective = canceled ? subscription.CanceledAtUtc ?? now : subscription.CurrentPeriodEndUtc ?? now;
            DateTime requested = source is "paddle" ? subscription.LastWebhookOccurredAtUtc ?? now : now;
            latest = BillingCancellation.Create(subscription, source, requested, effective);
            await cancellations.AddAsync(latest, cancellationToken);
        } else {
            latest.Synchronize(subscription, timeProvider.GetUtcNow().UtcDateTime);
            await cancellations.UpdateAsync(latest, cancellationToken);
        }
        return latest;
    }
}
