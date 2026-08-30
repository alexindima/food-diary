using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Services;

public sealed class BillingAccessService(
    IBillingUserContextService billingUserContextService,
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    TimeProvider dateTimeProvider) {
    public async Task EnsurePremiumRoleAsync(
        UserBillingProfileModel user,
        BillingSubscription subscription,
        bool shouldHavePremium,
        CancellationToken cancellationToken) {
        bool hasPremium = user.HasPaidPremium;
        if (hasPremium == shouldHavePremium) {
            if (shouldHavePremium && !subscription.PremiumRoleManagedByBilling) {
                return;
            }

            bool wasManagedByBilling = subscription.PremiumRoleManagedByBilling;
            subscription.MarkPremiumRoleManagedByBilling(shouldHavePremium, dateTimeProvider.GetUtcNow().UtcDateTime);
            if (subscription.PremiumRoleManagedByBilling != wasManagedByBilling) {
                await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        if (shouldHavePremium) {
            await billingUserContextService.EnsurePremiumRoleAsync(user.UserId, cancellationToken).ConfigureAwait(false);
            subscription.MarkPremiumRoleManagedByBilling(value: true, nowUtc);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        } else {
            if (!subscription.PremiumRoleManagedByBilling) {
                return;
            }

            await billingUserContextService.RemovePremiumRoleAsync(user.UserId, cancellationToken).ConfigureAwait(false);
            subscription.MarkPremiumRoleManagedByBilling(value: false, nowUtc);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
    }

    public bool ShouldHavePremiumAccess(string status, DateTime? currentPeriodEndUtc) {
        if (string.IsNullOrWhiteSpace(status)) {
            return false;
        }

        return status.Trim().ToLowerInvariant() switch {
            "trialing" => currentPeriodEndUtc.HasValue && currentPeriodEndUtc > dateTimeProvider.GetUtcNow().UtcDateTime,
            "active" => true,
            "past_due" => currentPeriodEndUtc.HasValue && currentPeriodEndUtc > dateTimeProvider.GetUtcNow().UtcDateTime,
            _ => false,
        };
    }
}
