using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using System.Globalization;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Application.Common;

namespace FoodDiary.Modules.Billing.Application.Commands.RenewDueSubscriptions;

public sealed class RenewDueSubscriptionsCommandHandler(
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    IBillingPaymentWriteRepository billingPaymentRepository,
    IUserBillingService billingUserContextService,
    IBillingTransactionRunner billingTransactionRunner,
    IEnumerable<IBillingRecurringProviderGateway> recurringProviderGateways,
    BillingAccessService billingAccessService,
    TimeProvider dateTimeProvider) : IRequestHandler<RenewDueSubscriptionsCommand, BillingRenewalRunResult> {
    private static readonly TimeSpan FailedRenewalRetryDelay = TimeSpan.FromHours(1);

    private readonly Dictionary<string, IBillingRecurringProviderGateway> _recurringGateways = recurringProviderGateways
        .ToDictionary(gateway => gateway.Provider, StringComparer.OrdinalIgnoreCase);

    private enum RenewalOutcome {
        Renewed = 0,
        Failed = 1,
        Skipped = 2,
    }

    public async Task<BillingRenewalRunResult> Handle(
        RenewDueSubscriptionsCommand request,
        CancellationToken cancellationToken) {
        if (!TryGetRecurringGateway(request.Provider, out IBillingRecurringProviderGateway recurringGateway)) {
            return new BillingRenewalRunResult(0, 0, 0);
        }

        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<BillingSubscription> subscriptions = await billingSubscriptionRepository.GetDueForRenewalAsync(
            recurringGateway.Provider,
            now,
            request.BatchSize,
            cancellationToken).ConfigureAwait(false);
        int renewed = 0;
        int failed = 0;

        foreach (BillingSubscription subscription in subscriptions) {
            cancellationToken.ThrowIfCancellationRequested();

            RenewalOutcome outcome = await ProcessSubscriptionRenewalAsync(
                subscription,
                recurringGateway,
                now,
                cancellationToken).ConfigureAwait(false);
            if (outcome == RenewalOutcome.Renewed) {
                renewed++;
            } else if (outcome == RenewalOutcome.Failed) {
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
        UserId userId = subscription.UserId;
        RenewalSnapshot? snapshot = null;
        await billingTransactionRunner.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(userId.Value), async ct => {
            // The batch may be stale after another payment or cancellation. Reload after acquiring the user lock.
            snapshot = null;
            BillingSubscription? current = await billingSubscriptionRepository.GetByUserIdAsync(userId, ct).ConfigureAwait(false);
            if (current is not null &&
                string.Equals(current.Provider, recurringGateway.Provider, StringComparison.OrdinalIgnoreCase) &&
                current.Status is "active" or "trialing" or "past_due" &&
                !current.CancelAtPeriodEnd && current.NextBillingAttemptUtc <= now) {
                subscription = current;
                snapshot = Capture(current);
            }
        }, cancellationToken).ConfigureAwait(false);
        if (snapshot is null) {
            return RenewalOutcome.Skipped;
        }
        if (HasIncompleteBillingDetails(subscription)) {
            await MarkRenewalFailedAsync(
                    snapshot,
                    "Renewal skipped because subscription billing details are incomplete.",
                    cancellationToken).ConfigureAwait(false);
            return RenewalOutcome.Failed;
        }

        UserBillingProfileModel? user = await billingUserContextService.GetProfileIncludingDeletedAsync(subscription.UserId, cancellationToken).ConfigureAwait(false);
        if (user is not { IsActive: true, IsDeleted: false }) {
            await SkipRenewalForInaccessibleUserAsync(snapshot, now, cancellationToken).ConfigureAwait(false);
            return RenewalOutcome.Failed;
        }

        Result<BillingRecurringPaymentModel> renewalResult = await ResolveRecurringPaymentAsync(snapshot, recurringGateway, cancellationToken).ConfigureAwait(false);
        if (renewalResult.IsFailure) {
            await MarkRenewalFailedAsync(snapshot, renewalResult.Error.Message, cancellationToken).ConfigureAwait(false);
            return RenewalOutcome.Failed;
        }

        try {
            await ApplyRenewalResultAsync(
                snapshot,
                renewalResult.Value,
                recurringGateway.Provider,
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
        RenewalSnapshot snapshot,
        DateTime skippedAtUtc,
        CancellationToken cancellationToken) {
        await billingTransactionRunner.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(snapshot.Request.UserId), async ct => {
            BillingSubscription subscription = await billingSubscriptionRepository.GetByUserIdAsync(new UserId(snapshot.Request.UserId), ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The subscription no longer exists.");
            if (Capture(subscription) != snapshot) {
                return;
            }
            subscription.MarkRenewalSkippedForInaccessibleUser(
                BuildRenewalSkippedEventId(subscription, skippedAtUtc),
                skippedAtUtc,
                SerializeReason("Renewal skipped because subscription user is not accessible."));
            if (subscription.PremiumRoleManagedByBilling) {
                subscription.MarkPremiumRoleManagedByBilling(value: false, skippedAtUtc);
            }

            await billingSubscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<BillingRecurringPaymentModel>> ResolveRecurringPaymentAsync(
        RenewalSnapshot snapshot,
        IBillingRecurringProviderGateway recurringGateway,
        CancellationToken cancellationToken) {
        BillingPayment? previous = string.IsNullOrWhiteSpace(snapshot.ExternalSubscriptionId)
            ? null
            : await billingPaymentRepository.GetByExternalPaymentIdAsync(snapshot.Provider, snapshot.ExternalSubscriptionId, cancellationToken).ConfigureAwait(false);
        BillingRecurringPaymentRequestModel request = snapshot.Request;
        if (previous is { Kind: BillingPaymentKinds.Renewal }) {
            // Also verify legacy past_due records, whose provider outcome was not distinguished.
            if (previous.Status is "pending" or "past_due") {
                return await recurringGateway.GetRecurringPaymentAsync(previous.ExternalPaymentId, request, cancellationToken).ConfigureAwait(false);
            }
            if (string.Equals(previous.Status, "canceled", StringComparison.Ordinal)) {
                // Only a confirmed final decline opens a new attempt. Transport failures retain this key.
                string nextAttempt = $"{request.IdempotenceKey}:after:{previous.ExternalPaymentId}";
                request = request with { IdempotenceKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nextAttempt))) };
            }
        }
        return await recurringGateway.CreateRecurringPaymentAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplyRenewalResultAsync(
        RenewalSnapshot snapshot,
        BillingRecurringPaymentModel renewal,
        string provider,
        DateTime renewedAtUtc,
        CancellationToken cancellationToken) {
        await billingTransactionRunner.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(snapshot.Request.UserId), async ct => {
            BillingSubscription subscription = await billingSubscriptionRepository.GetByUserIdAsync(new UserId(snapshot.Request.UserId), ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The subscription no longer exists.");
            bool isCurrent = Capture(subscription) == snapshot;
            await RecordRenewalPaymentAsync(snapshot, subscription, renewal, provider, ct).ConfigureAwait(false);
            if (!isCurrent) {
                return;
            }

            bool succeeded = string.Equals(renewal.Status, "active", StringComparison.OrdinalIgnoreCase);
            subscription.ApplyProviderSnapshot(
                provider,
                renewal.PaymentId,
                renewal.PaymentMethodId,
                renewal.PriceId,
                renewal.Plan,
                succeeded ? "active" : "past_due",
                succeeded ? renewal.CurrentPeriodStartUtc : subscription.CurrentPeriodStartUtc,
                succeeded ? renewal.CurrentPeriodEndUtc : subscription.CurrentPeriodEndUtc,
                cancelAtPeriodEnd: false,
                canceledAtUtc: null,
                trialStartUtc: null,
                trialEndUtc: null,
                renewal.EventId,
                renewedAtUtc,
                renewal.ProviderMetadataJson,
                renewal.OccurredAtUtc);
            if (!succeeded) {
                DateTime completedAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
                subscription.MarkRenewalFailed(completedAtUtc.Add(FailedRenewalRetryDelay), renewal.EventId, completedAtUtc, renewal.ProviderMetadataJson);
            }
            await billingSubscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

            UserBillingProfileModel? user = await billingUserContextService.GetProfileIncludingDeletedAsync(subscription.UserId, ct).ConfigureAwait(false);
            if (user is not { IsActive: true, IsDeleted: false }) {
                subscription.MarkRenewalSkippedForInaccessibleUser(
                    BuildRenewalSkippedEventId(subscription, renewedAtUtc),
                    renewedAtUtc,
                    SerializeReason("User became inaccessible while the renewal payment was being processed."));
                subscription.MarkPremiumRoleManagedByBilling(value: false, renewedAtUtc);
                await billingSubscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);
                return;
            }

            bool shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
                subscription.Status,
                subscription.CurrentPeriodEndUtc);
            await billingAccessService.EnsurePremiumRoleAsync(user, subscription, shouldHavePremium, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task RecordRenewalPaymentAsync(
        RenewalSnapshot snapshot,
        BillingSubscription subscription,
        BillingRecurringPaymentModel renewal,
        string provider,
        CancellationToken cancellationToken) {
        BillingPayment? existingPayment = await billingPaymentRepository.GetByExternalPaymentIdAsync(
            provider,
            renewal.PaymentId,
            cancellationToken).ConfigureAwait(false);
        if (existingPayment is not null) {
            // Payment history has its own ordering; a provider switch must not discard a verified payment outcome.
            bool isCurrentPayment = existingPayment.OccurredAtUtc is not { } recordedAt ||
                (renewal.OccurredAtUtc is { } receivedAt && receivedAt >= recordedAt);
            if (isCurrentPayment && existingPayment.Status is "pending" or "past_due") {
                existingPayment.ApplyProviderResult(existingPayment.BillingSubscriptionId,
                    snapshot.Request.CustomerId, renewal.PaymentId, renewal.PaymentMethodId,
                    renewal.PriceId, renewal.Plan, renewal.Status, BillingPaymentKinds.Renewal,
                    renewal.Amount, renewal.Currency, renewal.CurrentPeriodStartUtc, renewal.CurrentPeriodEndUtc,
                    renewal.EventId, renewal.ProviderMetadataJson, occurredAtUtc: renewal.OccurredAtUtc);
                await billingPaymentRepository.UpdateAsync(existingPayment, cancellationToken).ConfigureAwait(false);
            }
            return;
        }

        var payment = BillingPayment.Create(
            new UserId(snapshot.Request.UserId),
            subscription.Id == snapshot.Request.BillingSubscriptionId ? subscription.Id : null,
            provider,
            renewal.PaymentId,
            snapshot.Request.CustomerId,
            renewal.PaymentId,
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
            renewal.ProviderMetadataJson,
            occurredAtUtc: renewal.OccurredAtUtc);
        await billingPaymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildRenewalIdempotenceKey(BillingSubscription subscription) {
        string periodKey = subscription.CurrentPeriodEndUtc?.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) ?? "initial";
        return $"billing-renewal:{subscription.Id:N}:{periodKey}:{subscription.Plan}";
    }

    private static string BuildRenewalFailureEventId(BillingSubscription subscription, DateTime failedAtUtc) =>
        string.Create(CultureInfo.InvariantCulture, $"billing-renewal-failed:{subscription.Id:N}:{failedAtUtc:yyyyMMddHHmmss}");

    private static string BuildRenewalSkippedEventId(BillingSubscription subscription, DateTime skippedAtUtc) =>
        string.Create(CultureInfo.InvariantCulture, $"billing-renewal-skipped-inaccessible-user:{subscription.Id:N}:{skippedAtUtc:yyyyMMddHHmmss}");

    private async Task MarkRenewalFailedAsync(
        RenewalSnapshot snapshot,
        string reason,
        CancellationToken cancellationToken) {
        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        await billingTransactionRunner.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(snapshot.Request.UserId), async ct => {
            BillingSubscription subscription = await billingSubscriptionRepository.GetByUserIdAsync(new UserId(snapshot.Request.UserId), ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The subscription no longer exists.");
            if (Capture(subscription) != snapshot) {
                return;
            }
            subscription.MarkRenewalFailed(
                now.Add(FailedRenewalRetryDelay),
                BuildRenewalFailureEventId(subscription, now),
                now,
                SerializeReason(reason));
            await billingSubscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

            UserBillingProfileModel? failedRenewalUser = await billingUserContextService.GetProfileIncludingDeletedAsync(subscription.UserId, ct).ConfigureAwait(false);
            if (failedRenewalUser is { IsActive: true, IsDeleted: false }) {
                bool shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
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

    private static string SerializeReason(string reason) => JsonSerializer.Serialize(new { reason });

    private static RenewalSnapshot Capture(BillingSubscription subscription) => new(
        new BillingRecurringPaymentRequestModel(subscription.UserId.Value, subscription.Id,
            subscription.ExternalCustomerId, subscription.ExternalPaymentMethodId!, subscription.Plan!,
            subscription.CurrentPeriodEndUtc, BuildRenewalIdempotenceKey(subscription)),
        subscription.Provider, subscription.ExternalSubscriptionId, subscription.LastWebhookEventId,
        subscription.LastSyncedAtUtc, subscription.LastWebhookOccurredAtUtc, subscription.Status,
        subscription.CancelAtPeriodEnd, subscription.NextBillingAttemptUtc);

    private sealed record RenewalSnapshot(
        BillingRecurringPaymentRequestModel Request,
        string Provider,
        string? ExternalSubscriptionId,
        string? LastWebhookEventId,
        DateTime? LastSyncedAtUtc,
        DateTime? LastWebhookOccurredAtUtc,
        string Status,
        bool CancelAtPeriodEnd,
        DateTime? NextBillingAttemptUtc);
}
