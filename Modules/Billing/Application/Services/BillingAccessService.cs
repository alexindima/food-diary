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
            if (shouldHavePremium) {
                await EnsureRoleAsync(user, subscription, cancellationToken);
            }

            bool wasManagedByBilling = subscription.PremiumRoleManagedByBilling;
            subscription.MarkPremiumRoleManagedByBilling(shouldHavePremium, dateTimeProvider.GetUtcNow().UtcDateTime);
            if (subscription.PremiumRoleManagedByBilling != wasManagedByBilling) {
                await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken);
            }

            return;
        }

        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        if (shouldHavePremium) {
            await EnsureRoleAsync(user, subscription, cancellationToken);
            subscription.MarkPremiumRoleManagedByBilling(value: true, nowUtc);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken);
        } else {
            await RevokeManagedPremiumRoleAsync(subscription, cancellationToken);
        }
    }

    public async Task RevokeManagedPremiumRoleAsync(BillingSubscription subscription, CancellationToken cancellationToken) {
        if (!subscription.PremiumRoleManagedByBilling) {
            return;
        }

        await billingUserContextService.Send(new RemoveUserPremiumRoleCommand(UserId: subscription.UserId), cancellationToken);
        subscription.MarkPremiumRoleManagedByBilling(value: false, dateTimeProvider.GetUtcNow().UtcDateTime);
        await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken);
    }

    private Task<Unit> EnsureRoleAsync(UserBillingProfileModel user, BillingSubscription subscription, CancellationToken cancellationToken) =>
        billingUserContextService.Send(new EnsureUserPremiumRoleCommand(user.UserId,
            subscription.Provider is BillingProviderNames.TelegramStars ? subscription.CurrentPeriodEndUtc : null), cancellationToken);

    public bool ShouldHavePremiumAccess(string status, DateTime? currentPeriodEndUtc, string? provider = null) =>
        BillingPremiumAccessPolicy.GrantsPremiumAccess(status, currentPeriodEndUtc, dateTimeProvider.GetUtcNow().UtcDateTime, provider);
}
