using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Domain.Entities.Billing;
using User = FoodDiary.Domain.Entities.Users.User;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookPremiumRoleSyncer(
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    IBillingUserContextService billingUserContextService,
    BillingAccessService billingAccessService,
    IMarketingConversionRecorder marketingConversionRecorder,
    TimeProvider dateTimeProvider) {
    public async Task SyncAsync(
        User user,
        BillingSubscription subscription,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        bool shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
            webhookEvent.Status,
            webhookEvent.CurrentPeriodEndUtc);
        bool canAccess = await billingUserContextService.CanAccessUserAsync(user, cancellationToken).ConfigureAwait(false);
        if (canAccess) {
            await billingAccessService.EnsurePremiumRoleAsync(user, subscription, shouldHavePremium, cancellationToken).ConfigureAwait(false);
            if (shouldHavePremium) {
                await marketingConversionRecorder.RecordPremiumStartedAsync(user.Id.Value, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (subscription.PremiumRoleManagedByBilling) {
            subscription.MarkPremiumRoleManagedByBilling(value: false, dateTimeProvider.GetUtcNow().UtcDateTime);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
    }
}
