using System.Globalization;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Billing;
using User = FoodDiary.Domain.Entities.Users.User;

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

    private enum RenewalOutcome {
        Renewed,
        Failed
    }

    public async Task<BillingRenewalRunResult> RenewDueSubscriptionsAsync(
        string provider,
        int batchSize,
        CancellationToken cancellationToken = default) {
        if (!TryGetRecurringGateway(provider, out var recurringGateway)) {
            return new BillingRenewalRunResult(0, 0, 0);
        }

        var now = dateTimeProvider.UtcNow;
        var subscriptions = await billingSubscriptionRepository.GetDueForRenewalAsync(
            recurringGateway.Provider,
            now,
            batchSize,
            cancellationToken).ConfigureAwait(false);
        var renewed = 0;
        var failed = 0;

        foreach (var subscription in subscriptions) {
            cancellationToken.ThrowIfCancellationRequested();

            var outcome = await ProcessSubscriptionRenewalAsync(
                subscription,
                recurringGateway,
                now,
                cancellationToken).ConfigureAwait(false);
            if (outcome == RenewalOutcome.Renewed) {
                renewed++;
            } else {
                failed++;
            }
        }

        return new BillingRenewalRunResult(subscriptions.Count, renewed, failed);
    }

    private bool TryGetRecurringGateway(
        string provider,
        out IBillingRecurringProviderGateway recurringGateway) {
        if (!string.IsNullOrWhiteSpace(provider) &&
            _recurringGateways.TryGetValue(provider.Trim(), out recurringGateway!)) {
            return true;
        }

        recurringGateway = null!;
        return false;
    }

    private async Task<RenewalOutcome> ProcessSubscriptionRenewalAsync(
        BillingSubscription subscription,
        IBillingRecurringProviderGateway recurringGateway,
        DateTime now,
        CancellationToken cancellationToken) {
        if (HasIncompleteBillingDetails(subscription)) {
            await MarkRenewalFailedAsync(
                    subscription,
                    "Renewal skipped because subscription billing details are incomplete.",
                    cancellationToken).ConfigureAwait(false);
            return RenewalOutcome.Failed;
        }

        var user = await userRepository.GetByIdIncludingDeletedAsync(subscription.UserId, cancellationToken).ConfigureAwait(false);
        if (CurrentUserAccessPolicy.EnsureCanAccess(user) is not null) {
            await SkipRenewalForInaccessibleUserAsync(subscription, now, cancellationToken).ConfigureAwait(false);
            return RenewalOutcome.Failed;
        }

        var renewalResult = await CreateRecurringPaymentAsync(subscription, recurringGateway, cancellationToken).ConfigureAwait(false);
        if (renewalResult.IsFailure) {
            await MarkRenewalFailedAsync(subscription, renewalResult.Error.Message, cancellationToken).ConfigureAwait(false);
            return RenewalOutcome.Failed;
        }

        try {
            await ApplySuccessfulRenewalAsync(
                subscription,
                renewalResult.Value,
                recurringGateway.Provider,
                user!,
                now,
                cancellationToken).ConfigureAwait(false);
        } catch (BillingPaymentAlreadyExistsException) {
            return RenewalOutcome.Renewed;
        }

        return string.Equals(renewalResult.Value.Status, "active", StringComparison.OrdinalIgnoreCase)
            ? RenewalOutcome.Renewed
            : RenewalOutcome.Failed;
    }

    private static bool HasIncompleteBillingDetails(BillingSubscription subscription) =>
        string.IsNullOrWhiteSpace(subscription.ExternalPaymentMethodId) ||
        string.IsNullOrWhiteSpace(subscription.Plan);

    private async Task SkipRenewalForInaccessibleUserAsync(
        BillingSubscription subscription,
        DateTime skippedAtUtc,
        CancellationToken cancellationToken) {
        await billingTransactionRunner.ExecuteAsync(async ct => {
            subscription.MarkRenewalSkippedForInaccessibleUser(
                BuildRenewalSkippedEventId(subscription, skippedAtUtc),
                skippedAtUtc,
                "Renewal skipped because subscription user is not accessible.");
            if (subscription.PremiumRoleManagedByBilling) {
                subscription.MarkPremiumRoleManagedByBilling(false, skippedAtUtc);
            }

            await billingSubscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
        BillingSubscription subscription,
        IBillingRecurringProviderGateway recurringGateway,
        CancellationToken cancellationToken) =>
        recurringGateway.CreateRecurringPaymentAsync(
            new BillingRecurringPaymentRequestModel(
                subscription.UserId.Value,
                subscription.Id,
                subscription.ExternalCustomerId,
                subscription.ExternalPaymentMethodId!,
                subscription.Plan!,
                subscription.CurrentPeriodEndUtc,
                BuildRenewalIdempotenceKey(subscription)),
            cancellationToken);

    private async Task ApplySuccessfulRenewalAsync(
        BillingSubscription subscription,
        BillingRecurringPaymentModel renewal,
        string provider,
        User user,
        DateTime renewedAtUtc,
        CancellationToken cancellationToken) {
        await billingTransactionRunner.ExecuteAsync(async ct => {
            subscription.ApplyProviderSnapshot(
                provider,
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
                renewedAtUtc,
                renewal.ProviderMetadataJson);
            await billingSubscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

            await AddRenewalPaymentIfMissingAsync(subscription, renewal, provider, ct).ConfigureAwait(false);

            var shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
                renewal.Status,
                renewal.CurrentPeriodEndUtc);
            await billingAccessService.EnsurePremiumRoleAsync(user, subscription, shouldHavePremium, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task AddRenewalPaymentIfMissingAsync(
        BillingSubscription subscription,
        BillingRecurringPaymentModel renewal,
        string provider,
        CancellationToken cancellationToken) {
        var existingPayment = await billingPaymentRepository.GetByExternalPaymentIdAsync(
            provider,
            renewal.PaymentId,
            cancellationToken).ConfigureAwait(false);
        if (existingPayment is not null) {
            return;
        }

        var payment = BillingPayment.Create(
            subscription.UserId,
            subscription.Id,
            provider,
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
        await billingPaymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
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
            await billingSubscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

            var failedRenewalUser = await userRepository.GetByIdAsync(subscription.UserId, ct).ConfigureAwait(false);
            if (failedRenewalUser is not null) {
                var shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
                    subscription.Status,
                    subscription.CurrentPeriodEndUtc);
                await billingAccessService.EnsurePremiumRoleAsync(
                    failedRenewalUser,
                    subscription,
                    shouldHavePremium,
                    ct).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}
