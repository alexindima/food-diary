using FoodDiary.Application.Abstractions.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;

public sealed class CreateCheckoutSessionCommandHandler(
    ISender billingUserContextService,
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    IBillingPaymentWriteRepository billingPaymentRepository,
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor,
    TimeProvider dateTimeProvider,
    IBillingCheckoutLock billingCheckoutLock,
    IBillingTransactionRunner transactionRunner)
    : IRequestHandler<CreateCheckoutSessionCommand, Result<BillingCheckoutSessionModel>> {
    public async Task<Result<BillingCheckoutSessionModel>> Handle(
        CreateCheckoutSessionCommand request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await ResolveUserIdAsync(request, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return BillingCurrentUserAccessResolver.ToFailure<BillingCheckoutSessionModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        IAsyncDisposable lockHandle = await billingCheckoutLock
            .AcquireAsync(userId.Value, cancellationToken)
            .ConfigureAwait(false);
        await using ConfiguredAsyncDisposable checkoutLock = lockHandle.ConfigureAwait(false);
        Result<UserBillingProfileModel> userResult = await billingUserContextService.Send(new GetUserBillingProfileQuery(UserId: userId), cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<BillingCheckoutSessionModel>(userResult.Error);
        }

        UserBillingProfileModel user = userResult.Value;
        if (string.IsNullOrWhiteSpace(user.Email) || !user.IsEmailConfirmed) {
            return Result.Failure<BillingCheckoutSessionModel>(UserErrors.EmailRequired);
        }
        BillingSubscription? existingSubscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user.HasPaidPremium || IsPaidPremiumActive(existingSubscription, dateTimeProvider.GetUtcNow().UtcDateTime)) {
            return Result.Failure<BillingCheckoutSessionModel>(BillingErrors.SubscriptionAlreadyActive);
        }

        if (IsCheckoutInProgress(existingSubscription, dateTimeProvider.GetUtcNow().UtcDateTime)) {
            return Result.Failure<BillingCheckoutSessionModel>(BillingErrors.CheckoutAlreadyInProgress);
        }

        IBillingProviderGateway? billingProvider = ResolveBillingProvider(request.Provider);
        if (billingProvider is null) {
            return Result.Failure<BillingCheckoutSessionModel>(
                BillingErrors.ProviderNotConfigured(request.Provider ?? string.Empty));
        }

        string plan = request.Plan.Trim().ToLowerInvariant();
        CheckoutVersion? originalVersion = CaptureVersion(existingSubscription);
        Result<BillingCheckoutSessionModel> sessionResult = await billingProvider.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(
                userId.Value,
                user.Email,
                plan,
                string.Equals(existingSubscription?.Provider, billingProvider.Provider, StringComparison.OrdinalIgnoreCase)
                    ? existingSubscription?.ExternalCustomerId
                    : null,
                ResolveIdempotencyKey(request.IdempotencyKey, userId, plan)),
            cancellationToken).ConfigureAwait(false);
        if (sessionResult.IsFailure) {
            return Result.Failure<BillingCheckoutSessionModel>(sessionResult.Error);
        }

        BillingCheckoutSessionModel session = sessionResult.Value;

        var result = Result.Success(session);
        await transactionRunner.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(userId.Value), async token => {
            // The transaction resets tracking; never save the entity read before provider HTTP.
            Result<UserBillingProfileModel> currentUser = await billingUserContextService.Send(new GetUserBillingProfileQuery(UserId: userId), token).ConfigureAwait(false);
            if (currentUser.IsFailure) {
                result = Result.Failure<BillingCheckoutSessionModel>(currentUser.Error);
                return;
            }
            BillingSubscription? current = await billingSubscriptionRepository.GetByUserIdAsync(userId, token).ConfigureAwait(false);
            if (currentUser.Value.HasPaidPremium || IsPaidPremiumActive(current, dateTimeProvider.GetUtcNow().UtcDateTime)) {
                result = Result.Failure<BillingCheckoutSessionModel>(BillingErrors.SubscriptionAlreadyActive);
                return;
            }
            if (CaptureVersion(current) != originalVersion) {
                result = Result.Failure<BillingCheckoutSessionModel>(BillingErrors.CheckoutAlreadyInProgress);
                return;
            }
            BillingPayment? savedCheckout = await billingPaymentRepository.GetByExternalPaymentIdAsync(
                billingProvider.Provider, session.SessionId, token).ConfigureAwait(false);
            if (savedCheckout is not null) {
                bool matches = current is not null && savedCheckout.UserId == userId &&
                    savedCheckout.BillingSubscriptionId == current.Id &&
                    string.Equals(savedCheckout.Kind, BillingPaymentKinds.Checkout, StringComparison.Ordinal) &&
                    string.Equals(savedCheckout.Status, BillingSubscription.PendingCheckoutStatus, StringComparison.Ordinal) &&
                    string.Equals(current.Status, BillingSubscription.PendingCheckoutStatus, StringComparison.Ordinal) &&
                    string.Equals(current.Provider, billingProvider.Provider, StringComparison.Ordinal) &&
                    string.Equals(savedCheckout.ExternalCustomerId, session.CustomerId, StringComparison.Ordinal) && string.Equals(current.ExternalCustomerId, session.CustomerId, StringComparison.Ordinal) &&
                    string.Equals(savedCheckout.ExternalPriceId, session.PriceId, StringComparison.Ordinal) && string.Equals(current.ExternalPriceId, session.PriceId, StringComparison.Ordinal) &&
                    string.Equals(savedCheckout.Plan, session.Plan, StringComparison.Ordinal) && string.Equals(current.Plan, session.Plan, StringComparison.Ordinal);
                result = matches ? Result.Success(session)
                    : Result.Failure<BillingCheckoutSessionModel>(BillingErrors.CheckoutAlreadyInProgress);
                return;
            }
            result = Result.Success(session);
            if (current is null) {
                var pendingSubscription = BillingSubscription.CreatePending(
                    userId,
                    billingProvider.Provider,
                    session.CustomerId,
                    session.PriceId,
                    session.Plan);
                await billingSubscriptionRepository.AddAsync(pendingSubscription, token).ConfigureAwait(false);
                await AddCheckoutPaymentAsync(pendingSubscription, billingProvider.Provider, session, token).ConfigureAwait(false);
            } else {
                current.UpdateCheckoutContext(
                    billingProvider.Provider,
                    session.CustomerId,
                    session.PriceId,
                    session.Plan);
                await billingSubscriptionRepository.UpdateAsync(current, token).ConfigureAwait(false);
                await AddCheckoutPaymentAsync(current, billingProvider.Provider, session, token).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private sealed record CheckoutVersion(Guid Id, string Provider, string Status, DateTime? Modified,
        string? EventId, DateTime? Synced, DateTime? Occurred);

    private static CheckoutVersion? CaptureVersion(BillingSubscription? subscription) =>
        subscription is null ? null : new(subscription.Id, subscription.Provider, subscription.Status, subscription.ModifiedOnUtc,
            subscription.LastWebhookEventId, subscription.LastSyncedAtUtc, subscription.LastWebhookOccurredAtUtc);

    private Task<Result<UserId>> ResolveUserIdAsync(
        CreateCheckoutSessionCommand command,
        CancellationToken cancellationToken) =>
        BillingCurrentUserAccessResolver.ResolveAsync(command.UserId, billingUserContextService, cancellationToken);

    private IBillingProviderGateway? ResolveBillingProvider(string? provider) {
        string? normalizedProvider = provider?.Trim();
        return string.IsNullOrWhiteSpace(normalizedProvider)
            ? billingProviderGatewayAccessor.GetActiveProvider()
            : billingProviderGatewayAccessor.GetProviderOrDefault(normalizedProvider);
    }

    private static string ResolveIdempotencyKey(string? idempotencyKey, UserId userId, string plan) {
        if (!string.IsNullOrWhiteSpace(idempotencyKey)) {
            return idempotencyKey.Trim();
        }

        byte[] fallbackHash = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId.Value:N}:{plan}"));
        return $"checkout-{Convert.ToHexString(fallbackHash).ToLowerInvariant()}";
    }

    private static bool IsPaidPremiumActive(BillingSubscription? subscription, DateTime nowUtc) =>
        BillingPremiumAccessPolicy.GrantsPremiumAccess(subscription?.Status, subscription?.CurrentPeriodEndUtc, nowUtc);

    private static bool IsCheckoutInProgress(BillingSubscription? subscription, DateTime nowUtc) {
        if (subscription is null ||
            !string.Equals(subscription.Status, BillingSubscription.PendingCheckoutStatus, StringComparison.Ordinal)) {
            return false;
        }

        DateTime lastChangedUtc = subscription.ModifiedOnUtc ?? subscription.CreatedOnUtc;
        return lastChangedUtc > nowUtc.AddMinutes(-15);
    }

    private async Task AddCheckoutPaymentAsync(
        BillingSubscription subscription,
        string provider,
        BillingCheckoutSessionModel session,
        CancellationToken cancellationToken) {
        var payment = BillingPayment.Create(
            subscription.UserId,
            subscription.Id,
            provider,
            session.SessionId,
            session.CustomerId,
            externalSubscriptionId: null,
            externalPaymentMethodId: null,
            session.PriceId,
            session.Plan,
            BillingSubscription.PendingCheckoutStatus,
            BillingPaymentKinds.Checkout,
            amount: null,
            currency: null,
            currentPeriodStartUtc: null,
            currentPeriodEndUtc: null,
            webhookEventId: null,
            providerMetadataJson: null);
        await billingPaymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
    }
}
