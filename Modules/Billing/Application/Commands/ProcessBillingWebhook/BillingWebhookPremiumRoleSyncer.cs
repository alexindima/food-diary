using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Contracts.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookPremiumRoleSyncer(
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    BillingAccessService billingAccessService,
    IBillingMarketingConversionRecorder marketingConversionRecorder,
    TimeProvider dateTimeProvider) {
    public async Task SyncAsync(
        UserBillingProfileModel user,
        BillingSubscription subscription,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        bool shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
            subscription.Status,
            subscription.CurrentPeriodEndUtc);
        bool canAccess = user.IsActive && !user.IsDeleted;
        if (canAccess) {
            await billingAccessService.EnsurePremiumRoleAsync(user, subscription, shouldHavePremium, cancellationToken).ConfigureAwait(false);
            if (shouldHavePremium) {
                await marketingConversionRecorder.RecordPremiumStartedAsync(user.UserId.Value, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (subscription.PremiumRoleManagedByBilling) {
            subscription.MarkPremiumRoleManagedByBilling(value: false, dateTimeProvider.GetUtcNow().UtcDateTime);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
    }
}
