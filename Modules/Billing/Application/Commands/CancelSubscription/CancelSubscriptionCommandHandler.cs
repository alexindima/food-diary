using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.CancelSubscription;

public sealed class CancelSubscriptionCommandHandler(
    ISender sender,
    IBillingSubscriptionWriteRepository subscriptions,
    IBillingProviderGatewayAccessor gateways,
    IBillingCheckoutLock checkoutLock,
    IBillingTransactionRunner transactionRunner,
    BillingCancellationRecorder recorder,
    BillingAccessService access,
    TimeProvider timeProvider) : IRequestHandler<CancelSubscriptionCommand, Result<BillingCancellationModel>> {
    public async Task<Result<BillingCancellationModel>> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken) {
        Result<UserId> owner = await BillingCurrentUserAccessResolver.ResolveAsync(request.UserId, sender, cancellationToken);
        if (owner.IsFailure) {
            return Result.Failure<BillingCancellationModel>(owner.Error);
        }
        UserId userId = owner.Value;
        await using IAsyncDisposable lease = await checkoutLock.AcquireAsync(userId.Value, cancellationToken);
        BillingSubscription? before = await subscriptions.GetByUserIdAsync(userId, cancellationToken);
        if (before is not { Provider: BillingProviderNames.Paddle, ExternalSubscriptionId: { } externalId } ||
            !IsCancellationStateKnown(before.Status) ||
            string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(before.ExternalCustomerId)) {
            return Result.Failure<BillingCancellationModel>(BillingErrors.CancellationUnavailable);
        }
        IBillingProviderGateway? gateway = gateways.GetProviderOrDefault(before.Provider);
        if (gateway is null) {
            return Result.Failure<BillingCancellationModel>(BillingErrors.CancellationUnavailable);
        }
        Guid subscriptionId = before.Id;
        string? eventBeforeRequest = before.LastWebhookEventId;
        string customerId = before.ExternalCustomerId;
        Result<BillingCancellationSnapshotModel> provider = await gateway.CancelSubscriptionAsync(
            new BillingCancellationRequestModel(externalId, customerId), cancellationToken);
        if (provider.IsFailure) {
            return Result.Failure<BillingCancellationModel>(provider.Error);
        }
        Guid? cancellationId = null;
        Error? error = null;
        await transactionRunner.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(userId.Value), async token => {
            error = null;
            cancellationId = null;
            BillingSubscription? current = await subscriptions.GetByUserIdAsync(userId, token);
            Result<UserBillingProfileModel> user = await sender.Send(new GetUserBillingProfileQuery(userId), token);
            if (user.IsFailure) {
                error = user.Error;
                return;
            }
            if (current is null || !IsCancellationStateKnown(current.Status) || current.Id != subscriptionId || !string.Equals(current.Provider, BillingProviderNames.Paddle, StringComparison.Ordinal) ||
                !string.Equals(current.ExternalSubscriptionId, externalId, StringComparison.Ordinal) || !string.Equals(current.ExternalCustomerId, customerId, StringComparison.Ordinal)) {
                error = BillingErrors.CancellationUnavailable;
                return;
            }
            BillingCancellationSnapshotModel snapshot = provider.Value;
            if (current.LastWebhookOccurredAtUtc > snapshot.ObservedAtUtc ||
                (current.LastWebhookOccurredAtUtc == snapshot.ObservedAtUtc &&
                 !string.Equals(current.LastWebhookEventId, eventBeforeRequest, StringComparison.Ordinal))) {
                // A newer signed webhook (including a resumed renewal) wins over the older HTTP response.
                if (!current.CancelAtPeriodEnd && current.Status is not "canceled") {
                    error = BillingErrors.CancellationUnavailable;
                    return;
                }
            } else {
                current.ApplyProviderSnapshot(current.Provider, externalId, current.ExternalPaymentMethodId, current.ExternalPriceId,
                    current.Plan, snapshot.Status, snapshot.CurrentPeriodStartUtc, snapshot.CurrentPeriodEndUtc,
                    snapshot.CancelAtPeriodEnd, snapshot.CanceledAtUtc, current.TrialStartUtc, current.TrialEndUtc,
                    "api-cancel:" + snapshot.ObservedAtUtc.Ticks, timeProvider.GetUtcNow().UtcDateTime,
                    current.ProviderMetadataJson, snapshot.ObservedAtUtc);
                await subscriptions.UpdateAsync(current, token);
                await access.EnsurePremiumRoleAsync(user.Value, current,
                    access.ShouldHavePremiumAccess(current.Status, current.CurrentPeriodEndUtc), token);
            }
            BillingCancellation? cancellation = await recorder.SynchronizeAsync(current, "fooddiary", token);
            cancellationId = cancellation?.Id;
        }, cancellationToken);
        if (error is not null || cancellationId is null) {
            return Result.Failure<BillingCancellationModel>(error ?? BillingErrors.CancellationUnavailable);
        }
        Result<BillingOverviewModel> overview = await sender.Send(new GetBillingOverviewQuery(userId.Value), cancellationToken);
        return overview.IsFailure
            ? Result.Failure<BillingCancellationModel>(overview.Error)
            : Result.Success(new BillingCancellationModel(cancellationId.Value, overview.Value));
    }

    private static bool IsCancellationStateKnown(string status) =>
        status is "active" or "trialing" or "past_due" or "paused" or "canceled";

}
