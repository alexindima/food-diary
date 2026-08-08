using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookPaymentRecorder(IBillingPaymentWriteRepository billingPaymentRepository) {
    public async Task AddIfPresentAsync(
        BillingSubscription? subscription,
        UserId userId,
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        if (!webhookEvent.Amount.HasValue) {
            return;
        }

        string externalPaymentId = webhookEvent.ExternalPaymentId ??
            webhookEvent.ExternalSubscriptionId ??
            webhookEvent.ExternalPaymentMethodId ??
            webhookEvent.EventId;
        BillingPayment? existingPayment = await billingPaymentRepository.GetByExternalPaymentIdAsync(
            provider,
            externalPaymentId,
            cancellationToken).ConfigureAwait(false);
        if (existingPayment is not null) {
            existingPayment.ApplyProviderResult(
                subscription?.Id,
                webhookEvent.ExternalCustomerId,
                webhookEvent.ExternalSubscriptionId,
                webhookEvent.ExternalPaymentMethodId,
                webhookEvent.ExternalPriceId,
                webhookEvent.Plan,
                webhookEvent.Status,
                ResolvePaymentKind(webhookEvent),
                webhookEvent.Amount,
                webhookEvent.Currency,
                webhookEvent.CurrentPeriodStartUtc,
                webhookEvent.CurrentPeriodEndUtc,
                webhookEvent.EventId,
                webhookEvent.ProviderMetadataJson,
                webhookEvent.Tax,
                webhookEvent.Fee,
                webhookEvent.Earnings,
                webhookEvent.PayoutCurrency,
                webhookEvent.PayoutEarnings,
                webhookEvent.OccurredAtUtc);
            await billingPaymentRepository.UpdateAsync(existingPayment, cancellationToken).ConfigureAwait(false);
            return;
        }

        var payment = BillingPayment.Create(
            userId,
            subscription?.Id,
            provider,
            externalPaymentId,
            webhookEvent.ExternalCustomerId,
            webhookEvent.ExternalSubscriptionId,
            webhookEvent.ExternalPaymentMethodId,
            webhookEvent.ExternalPriceId,
            webhookEvent.Plan,
            webhookEvent.Status,
            ResolvePaymentKind(webhookEvent),
            webhookEvent.Amount,
            webhookEvent.Currency,
            webhookEvent.CurrentPeriodStartUtc,
            webhookEvent.CurrentPeriodEndUtc,
            webhookEvent.EventId,
            webhookEvent.ProviderMetadataJson,
            webhookEvent.Tax,
            webhookEvent.Fee,
            webhookEvent.Earnings,
            webhookEvent.PayoutCurrency,
            webhookEvent.PayoutEarnings,
            webhookEvent.OccurredAtUtc);
        await billingPaymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolvePaymentKind(BillingWebhookEventModel webhookEvent) {
        if (webhookEvent.FinancialAction is not null) {
            return webhookEvent.FinancialAction.Trim().ToLowerInvariant() switch {
                BillingPaymentKinds.Refund => BillingPaymentKinds.Refund,
                BillingPaymentKinds.Credit => BillingPaymentKinds.Credit,
                BillingPaymentKinds.Chargeback => BillingPaymentKinds.Chargeback,
                BillingPaymentKinds.ChargebackReverse => BillingPaymentKinds.ChargebackReverse,
                BillingPaymentKinds.CreditReverse => BillingPaymentKinds.CreditReverse,
                _ => BillingPaymentKinds.Adjustment,
            };
        }

        return webhookEvent.ExternalPaymentId is not null
            ? BillingPaymentKinds.Transaction
            : BillingPaymentKinds.Webhook;
    }
}
