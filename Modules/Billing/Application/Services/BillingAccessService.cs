using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Commands.EnsureUserPremiumRole;
using FoodDiary.Modules.Users.Contracts.Commands.RemoveUserPremiumRole;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Services;

public sealed class BillingAccessService(
    ISender billingUserContextService,
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
            await billingUserContextService.Send(new EnsureUserPremiumRoleCommand(UserId: user.UserId), cancellationToken).ConfigureAwait(false);
            subscription.MarkPremiumRoleManagedByBilling(value: true, nowUtc);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        } else {
            if (!subscription.PremiumRoleManagedByBilling) {
                return;
            }

            await billingUserContextService.Send(new RemoveUserPremiumRoleCommand(UserId: user.UserId), cancellationToken).ConfigureAwait(false);
            subscription.MarkPremiumRoleManagedByBilling(value: false, nowUtc);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
    }

    public bool ShouldHavePremiumAccess(string status, DateTime? currentPeriodEndUtc) =>
        BillingPremiumAccessPolicy.GrantsPremiumAccess(status, currentPeriodEndUtc, dateTimeProvider.GetUtcNow().UtcDateTime);
}
