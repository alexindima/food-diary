using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Services;

public sealed class BillingRenewalService(
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingPaymentRepository billingPaymentRepository,
    IUserRepository userRepository,
    IEnumerable<IBillingRecurringProviderGateway> recurringProviderGateways,
    BillingAccessService billingAccessService,
    IDateTimeProvider dateTimeProvider) {
    private readonly Dictionary<string, IBillingRecurringProviderGateway> _recurringGateways = recurringProviderGateways
        .ToDictionary(gateway => gateway.Provider, StringComparer.OrdinalIgnoreCase);

    public async Task<BillingRenewalRunResult> RenewDueSubscriptionsAsync(
        string provider,
        int batchSize,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(provider) ||
            !_recurringGateways.TryGetValue(provider.Trim(), out var recurringGateway)) {
            return new BillingRenewalRunResult(0, 0, 0);
        }

        var now = dateTimeProvider.UtcNow;
        var subscriptions = await billingSubscriptionRepository.GetDueForRenewalAsync(
            recurringGateway.Provider,
            now,
            batchSize,
            cancellationToken);
        var renewed = 0;
        var failed = 0;

        foreach (var subscription in subscriptions) {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(subscription.ExternalPaymentMethodId) ||
                string.IsNullOrWhiteSpace(subscription.Plan)) {
                failed++;
                continue;
            }

            var renewalResult = await recurringGateway.CreateRecurringPaymentAsync(
                new BillingRecurringPaymentRequestModel(
                    subscription.UserId.Value,
                    subscription.ExternalCustomerId,
                    subscription.ExternalPaymentMethodId,
                    subscription.Plan,
                    subscription.CurrentPeriodEndUtc),
                cancellationToken);

            if (renewalResult.IsFailure) {
                failed++;
                continue;
            }

            var renewal = renewalResult.Value;
            subscription.ApplyProviderSnapshot(
                recurringGateway.Provider,
                renewal.PaymentId,
                renewal.PaymentMethodId,
                renewal.PriceId,
                renewal.Plan,
                renewal.Status,
                renewal.CurrentPeriodStartUtc,
                renewal.CurrentPeriodEndUtc,
                false,
                null,
                null,
                null,
                renewal.EventId,
                now,
                renewal.ProviderMetadataJson);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken);

            var payment = BillingPayment.Create(
                subscription.UserId,
                subscription.Id,
                recurringGateway.Provider,
                renewal.PaymentId,
                subscription.ExternalCustomerId,
                subscription.ExternalSubscriptionId,
                renewal.PaymentMethodId,
                renewal.PriceId,
                renewal.Plan,
                renewal.Status,
                BillingPaymentKinds.Renewal,
                renewal.Amount,
                renewal.Currency,
                renewal.CurrentPeriodStartUtc,
                renewal.CurrentPeriodEndUtc,
                renewal.EventId,
                renewal.ProviderMetadataJson);
            await billingPaymentRepository.AddAsync(payment, cancellationToken);

            var user = await userRepository.GetByIdAsync(subscription.UserId, cancellationToken);
            if (user is not null) {
                var shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
                    renewal.Status,
                    renewal.CurrentPeriodEndUtc);
                await billingAccessService.EnsurePremiumRoleAsync(user, shouldHavePremium, cancellationToken);
            }

            if (string.Equals(renewal.Status, "active", StringComparison.OrdinalIgnoreCase)) {
                renewed++;
            } else {
                failed++;
            }
        }

        return new BillingRenewalRunResult(subscriptions.Count, renewed, failed);
    }
}
