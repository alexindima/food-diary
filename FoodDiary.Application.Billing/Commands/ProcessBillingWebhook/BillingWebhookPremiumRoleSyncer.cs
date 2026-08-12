using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

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
            webhookEvent.Status,
            webhookEvent.CurrentPeriodEndUtc);
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
