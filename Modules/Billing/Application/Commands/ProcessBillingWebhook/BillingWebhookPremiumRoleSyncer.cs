using FoodDiary.Mediator;
using FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookPremiumRoleSyncer(
    BillingAccessService billingAccessService,
    ISender marketingConversionRecorder) {
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
                await marketingConversionRecorder.Send(new RecordPremiumConversionCommand(UserId: user.UserId.Value), cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (!shouldHavePremium) {
            await billingAccessService.RevokeManagedPremiumRoleAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
    }
}
