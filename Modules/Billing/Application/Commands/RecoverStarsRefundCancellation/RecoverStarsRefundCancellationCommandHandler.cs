using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.RecoverStarsRefundCancellation;

// Financial recovery must continue for inactive/deleted accounts without browser authorization or an overview response.
public sealed class RecoverStarsRefundCancellationCommandHandler(IBillingSubscriptionWriteRepository subscriptions,
    IBillingProviderGatewayAccessor gateways, IBillingCheckoutLock checkoutLock, IBillingTransactionRunner transactions,
    BillingAccessService access, BillingCancellationRecorder cancellations, TimeProvider clock)
    : IRequestHandler<RecoverStarsRefundCancellationCommand, Result<Unit>> {
    public async Task<Result<Unit>> Handle(RecoverStarsRefundCancellationCommand request, CancellationToken cancellationToken) {
        await using IAsyncDisposable lease = await checkoutLock.AcquireAsync(request.UserId.Value, cancellationToken);
        BillingSubscription? before = await subscriptions.GetByUserIdAsync(request.UserId, cancellationToken);
        if (!NeedsRecovery(before)) {
            return Result.Success(Unit.Value);
        }
        IBillingProviderGateway? gateway = gateways.GetProviderOrDefault(BillingProviderNames.TelegramStars);
        if (gateway is null) {
            return Result.Failure<Unit>(BillingErrors.CancellationUnavailable);
        }
        Result<BillingCancellationSnapshotModel> canceled = await gateway.CancelSubscriptionAsync(
            new BillingCancellationRequestModel(before!.ExternalSubscriptionId!, before.ExternalCustomerId,
                before.ExternalPaymentMethodId, before.CurrentPeriodStartUtc, before.CurrentPeriodEndUtc), cancellationToken);
        if (canceled.IsFailure) {
            return Result.Failure<Unit>(canceled.Error);
        }
        await transactions.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(request.UserId.Value), async token => {
            BillingSubscription? current = await subscriptions.GetByUserIdAsync(request.UserId, token);
            if (!NeedsRecovery(current) || current!.Id != before.Id ||
                !string.Equals(current.ExternalSubscriptionId, before.ExternalSubscriptionId, StringComparison.Ordinal)) {
                return;
            }
            DateTime now = clock.GetUtcNow().UtcDateTime;
            bool refunded = current.Status is "refunded";
            // A renewal confirmed during the HTTP call retains its newer paid period, with renewal disabled.
            current.ApplyProviderSnapshot(current.Provider, current.ExternalSubscriptionId, current.ExternalPaymentMethodId,
                current.ExternalPriceId, current.Plan, refunded ? "canceled" : current.Status,
                current.CurrentPeriodStartUtc, current.CurrentPeriodEndUtc, cancelAtPeriodEnd: true,
                canceledAtUtc: refunded ? now : current.CanceledAtUtc, current.TrialStartUtc, current.TrialEndUtc,
                "refund-cancel:" + now.Ticks, now, providerMetadataJson: null, webhookOccurredAtUtc: now);
            await subscriptions.UpdateAsync(current, token);
            if (refunded) {
                await access.RevokeManagedPremiumRoleAsync(current, token);
            }
            await cancellations.SynchronizeAsync(current, "telegram", token);
        }, cancellationToken);
        return Result.Success(Unit.Value);
    }

    private static bool NeedsRecovery(BillingSubscription? subscription) => subscription is {
        Provider: BillingProviderNames.TelegramStars, ExternalSubscriptionId: not null, ExternalPaymentMethodId: not null,
    } && string.Equals(subscription.ProviderMetadataJson, TelegramStarsBillingState.RefundCancellationPending, StringComparison.Ordinal);
}
