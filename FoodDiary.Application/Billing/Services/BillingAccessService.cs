using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Billing.Services;

public sealed class BillingAccessService(
    IUserRepository userRepository,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IDateTimeProvider dateTimeProvider) {
    public async Task EnsurePremiumRoleAsync(
        User user,
        BillingSubscription subscription,
        bool shouldHavePremium,
        CancellationToken cancellationToken) {
        var currentRoles = user.GetRoleNames().ToList();
        var hasPremium = currentRoles.Contains(RoleNames.Premium, StringComparer.Ordinal);
        if (hasPremium == shouldHavePremium) {
            if (shouldHavePremium && !subscription.PremiumRoleManagedByBilling) {
                return;
            }

            var wasManagedByBilling = subscription.PremiumRoleManagedByBilling;
            subscription.MarkPremiumRoleManagedByBilling(shouldHavePremium, dateTimeProvider.UtcNow);
            if (subscription.PremiumRoleManagedByBilling != wasManagedByBilling) {
                await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (shouldHavePremium) {
            currentRoles.Add(RoleNames.Premium);
            subscription.MarkPremiumRoleManagedByBilling(true, dateTimeProvider.UtcNow);
        } else {
            if (!subscription.PremiumRoleManagedByBilling) {
                return;
            }

            currentRoles.RemoveAll(role => string.Equals(role, RoleNames.Premium, StringComparison.Ordinal));
            subscription.MarkPremiumRoleManagedByBilling(false, dateTimeProvider.UtcNow);
        }

        var roleEntities = await userRepository.GetRolesByNamesAsync(currentRoles, cancellationToken).ConfigureAwait(false);
        user.ReplaceRoles(roleEntities);
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public bool ShouldHavePremiumAccess(string status, DateTime? currentPeriodEndUtc) {
        if (string.IsNullOrWhiteSpace(status)) {
            return false;
        }

        return status.Trim().ToLowerInvariant() switch {
            "trialing" => currentPeriodEndUtc.HasValue && currentPeriodEndUtc > dateTimeProvider.UtcNow,
            "active" => true,
            "past_due" => !currentPeriodEndUtc.HasValue || currentPeriodEndUtc > dateTimeProvider.UtcNow,
            _ => false,
        };
    }
}
