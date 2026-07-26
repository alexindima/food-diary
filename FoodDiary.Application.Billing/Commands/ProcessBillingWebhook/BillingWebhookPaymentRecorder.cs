using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookPaymentRecorder(IBillingPaymentWriteRepository billingPaymentRepository) {
    public async Task AddIfPresentAsync(
        BillingSubscription subscription,
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        if (!webhookEvent.Amount.HasValue) {
            return;
        }

        string externalPaymentId = webhookEvent.ExternalSubscriptionId ??
            webhookEvent.ExternalPaymentMethodId ??
            webhookEvent.EventId;
        BillingPayment? existingPayment = await billingPaymentRepository.GetByExternalPaymentIdAsync(
            provider,
            externalPaymentId,
            cancellationToken).ConfigureAwait(false);
        if (existingPayment is not null) {
            return;
        }

        var payment = BillingPayment.Create(
            subscription.UserId,
            subscription.Id,
            provider,
            externalPaymentId,
            webhookEvent.ExternalCustomerId,
            webhookEvent.ExternalSubscriptionId,
            webhookEvent.ExternalPaymentMethodId,
            webhookEvent.ExternalPriceId,
            webhookEvent.Plan,
            webhookEvent.Status,
            BillingPaymentKinds.Webhook,
            webhookEvent.Amount,
            webhookEvent.Currency,
            webhookEvent.CurrentPeriodStartUtc,
            webhookEvent.CurrentPeriodEndUtc,
            webhookEvent.EventId,
            webhookEvent.ProviderMetadataJson);
        await billingPaymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
    }
}
