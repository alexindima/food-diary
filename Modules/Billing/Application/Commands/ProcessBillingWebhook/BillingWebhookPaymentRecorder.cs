using FoodDiary.Modules.Billing.Domain.ValueObjects;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;

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
            if (existingPayment.OccurredAtUtc is { } lastOccurredAtUtc &&
                webhookEvent.OccurredAtUtc is { } occurredAtUtc && occurredAtUtc < lastOccurredAtUtc) {
                return;
            }

            existingPayment.ApplyProviderObservation(
            billingSubscriptionId: subscription?.TypedId,
            externalCustomerId: webhookEvent.ExternalCustomerId,
            externalSubscriptionId: webhookEvent.ExternalSubscriptionId,
            externalPaymentMethodId: webhookEvent.ExternalPaymentMethodId,
            externalPriceId: webhookEvent.ExternalPriceId,
            plan: webhookEvent.Plan,
            status: webhookEvent.Status,
            kind: ResolvePaymentKind(webhookEvent),
            financials: BillingPaymentFinancials.FromFields(
            amount: webhookEvent.Amount,
            currency: webhookEvent.Currency,
            tax: webhookEvent.Tax,
            fee: webhookEvent.Fee,
            earnings: webhookEvent.Earnings,
            payoutCurrency: webhookEvent.PayoutCurrency,
            payoutEarnings: webhookEvent.PayoutEarnings),
            currentPeriodStartUtc: webhookEvent.CurrentPeriodStartUtc,
            currentPeriodEndUtc: webhookEvent.CurrentPeriodEndUtc,
            webhookEventId: webhookEvent.EventId,
            providerMetadataJson: webhookEvent.ProviderMetadataJson,
            occurredAtUtc: webhookEvent.OccurredAtUtc);
            await billingPaymentRepository.UpdateAsync(existingPayment, cancellationToken).ConfigureAwait(false);
            return;
        }

        var payment = BillingPayment.CreateWithFinancials(
            userId: userId,
            billingSubscriptionId: subscription?.TypedId,
            provider: provider,
            externalPaymentId: externalPaymentId,
            externalCustomerId: webhookEvent.ExternalCustomerId,
            externalSubscriptionId: webhookEvent.ExternalSubscriptionId,
            externalPaymentMethodId: webhookEvent.ExternalPaymentMethodId,
            externalPriceId: webhookEvent.ExternalPriceId,
            plan: webhookEvent.Plan,
            status: webhookEvent.Status,
            kind: ResolvePaymentKind(webhookEvent),
            financials: BillingPaymentFinancials.FromFields(
            amount: webhookEvent.Amount,
            currency: webhookEvent.Currency,
            tax: webhookEvent.Tax,
            fee: webhookEvent.Fee,
            earnings: webhookEvent.Earnings,
            payoutCurrency: webhookEvent.PayoutCurrency,
            payoutEarnings: webhookEvent.PayoutEarnings),
            currentPeriodStartUtc: webhookEvent.CurrentPeriodStartUtc,
            currentPeriodEndUtc: webhookEvent.CurrentPeriodEndUtc,
            webhookEventId: webhookEvent.EventId,
            providerMetadataJson: webhookEvent.ProviderMetadataJson,
            occurredAtUtc: webhookEvent.OccurredAtUtc);
        await billingPaymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolvePaymentKind(BillingWebhookEventModel webhookEvent) {
        if (webhookEvent.IsRenewal) {
            return BillingPaymentKinds.Renewal;
        }
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
