using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
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
    IUserBillingService billingUserContextService,
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    IBillingPaymentWriteRepository billingPaymentRepository,
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor,
    TimeProvider dateTimeProvider,
    IBillingCheckoutLock billingCheckoutLock,
    IUnitOfWork? unitOfWork = null)
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
        Result<UserBillingProfileModel> userResult = await billingUserContextService.GetAccessibleProfileAsync(userId, cancellationToken).ConfigureAwait(false);
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

        if (existingSubscription is null) {
            var pendingSubscription = BillingSubscription.CreatePending(
                userId,
                billingProvider.Provider,
                session.CustomerId,
                session.PriceId,
                session.Plan);
            await billingSubscriptionRepository.AddAsync(pendingSubscription, cancellationToken).ConfigureAwait(false);
            await AddCheckoutPaymentAsync(pendingSubscription, billingProvider.Provider, session, cancellationToken).ConfigureAwait(false);
        } else {
            existingSubscription.UpdateCheckoutContext(
                billingProvider.Provider,
                session.CustomerId,
                session.PriceId,
                session.Plan);
            await billingSubscriptionRepository.UpdateAsync(existingSubscription, cancellationToken).ConfigureAwait(false);
            await AddCheckoutPaymentAsync(existingSubscription, billingProvider.Provider, session, cancellationToken).ConfigureAwait(false);
        }

        if (unitOfWork?.HasPendingChanges == true) {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(session);
    }

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
