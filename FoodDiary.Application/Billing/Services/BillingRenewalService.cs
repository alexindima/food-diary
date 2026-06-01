using System.Globalization;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Services;

public sealed class BillingRenewalService(
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingPaymentRepository billingPaymentRepository,
    IUserRepository userRepository,
    IBillingTransactionRunner billingTransactionRunner,
    IEnumerable<IBillingRecurringProviderGateway> recurringProviderGateways,
    BillingAccessService billingAccessService,
    IDateTimeProvider dateTimeProvider) {
    private static readonly TimeSpan FailedRenewalRetryDelay = TimeSpan.FromHours(1);

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
                await MarkRenewalFailedAsync(
                    subscription,
                    "Renewal skipped because subscription billing details are incomplete.",
                    cancellationToken);
                failed++;
                continue;
            }

            var user = await userRepository.GetByIdIncludingDeletedAsync(subscription.UserId, cancellationToken);
            if (CurrentUserAccessPolicy.EnsureCanAccess(user) is not null) {
                await billingTransactionRunner.ExecuteAsync(async ct => {
                    subscription.MarkRenewalSkippedForInaccessibleUser(
                        BuildRenewalSkippedEventId(subscription, now),
                        now,
                        "Renewal skipped because subscription user is not accessible.");
                    if (subscription.PremiumRoleManagedByBilling) {
                        subscription.MarkPremiumRoleManagedByBilling(false, now);
                    }

                    await billingSubscriptionRepository.UpdateAsync(subscription, ct);
                }, cancellationToken);

                failed++;
                continue;
            }

            var renewalResult = await recurringGateway.CreateRecurringPaymentAsync(
                new BillingRecurringPaymentRequestModel(
                    subscription.UserId.Value,
                    subscription.Id,
                    subscription.ExternalCustomerId,
                    subscription.ExternalPaymentMethodId,
                    subscription.Plan,
                    subscription.CurrentPeriodEndUtc,
                    BuildRenewalIdempotenceKey(subscription)),
                cancellationToken);

            if (renewalResult.IsFailure) {
                await MarkRenewalFailedAsync(subscription, renewalResult.Error.Message, cancellationToken);
                failed++;
                continue;
            }

            var renewal = renewalResult.Value;
            try {
                await billingTransactionRunner.ExecuteAsync(async ct => {
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
                    await billingSubscriptionRepository.UpdateAsync(subscription, ct);

                    var existingPayment = await billingPaymentRepository.GetByExternalPaymentIdAsync(
                        recurringGateway.Provider,
                        renewal.PaymentId,
                        ct);
                    if (existingPayment is null) {
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
                        await billingPaymentRepository.AddAsync(payment, ct);
                    }

                    var shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
                        renewal.Status,
                        renewal.CurrentPeriodEndUtc);
                    await billingAccessService.EnsurePremiumRoleAsync(user!, subscription, shouldHavePremium, ct);
                }, cancellationToken);
            } catch (BillingPaymentAlreadyExistsException) {
                renewed++;
                continue;
            }

            if (string.Equals(renewal.Status, "active", StringComparison.OrdinalIgnoreCase)) {
                renewed++;
            } else {
                failed++;
            }
        }

        return new BillingRenewalRunResult(subscriptions.Count, renewed, failed);
    }

    private static string BuildRenewalIdempotenceKey(BillingSubscription subscription) {
        var periodKey = subscription.CurrentPeriodEndUtc?.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) ?? "initial";
        return $"billing-renewal:{subscription.Id:N}:{periodKey}:{subscription.Plan}";
    }

    private static string BuildRenewalFailureEventId(BillingSubscription subscription, DateTime failedAtUtc) =>
        $"billing-renewal-failed:{subscription.Id:N}:{failedAtUtc:yyyyMMddHHmmss}";

    private static string BuildRenewalSkippedEventId(BillingSubscription subscription, DateTime skippedAtUtc) =>
        $"billing-renewal-skipped-inaccessible-user:{subscription.Id:N}:{skippedAtUtc:yyyyMMddHHmmss}";

    private async Task MarkRenewalFailedAsync(
        BillingSubscription subscription,
        string reason,
        CancellationToken cancellationToken) {
        var now = dateTimeProvider.UtcNow;
        await billingTransactionRunner.ExecuteAsync(async ct => {
            subscription.MarkRenewalFailed(
                now.Add(FailedRenewalRetryDelay),
                BuildRenewalFailureEventId(subscription, now),
                now,
                reason);
            await billingSubscriptionRepository.UpdateAsync(subscription, ct);

            var failedRenewalUser = await userRepository.GetByIdAsync(subscription.UserId, ct);
            if (failedRenewalUser is not null) {
                var shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
                    subscription.Status,
                    subscription.CurrentPeriodEndUtc);
                await billingAccessService.EnsurePremiumRoleAsync(
                    failedRenewalUser,
                    subscription,
                    shouldHavePremium,
                    ct);
            }
        }, cancellationToken);
    }
}
