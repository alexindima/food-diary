using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Commands.CreateCheckoutSession;

public sealed class CreateCheckoutSessionCommandHandler(
    IUserRepository userRepository,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingPaymentRepository billingPaymentRepository,
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateCheckoutSessionCommand, Result<BillingCheckoutSessionModel>> {
    public async Task<Result<BillingCheckoutSessionModel>> Handle(
        CreateCheckoutSessionCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<BillingCheckoutSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<BillingCheckoutSessionModel>(accessError);
        }

        var existingSubscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user!.HasRole(RoleNames.Premium) || IsPaidPremiumActive(existingSubscription, dateTimeProvider.UtcNow)) {
            return Result.Failure<BillingCheckoutSessionModel>(Errors.Billing.SubscriptionAlreadyActive);
        }

        var billingProvider = ResolveBillingProvider(command.Provider);
        if (billingProvider is null) {
            return Result.Failure<BillingCheckoutSessionModel>(
                Errors.Billing.ProviderNotConfigured(command.Provider ?? string.Empty));
        }

        var plan = command.Plan.Trim().ToLowerInvariant();
        var sessionResult = await billingProvider.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(
                command.UserId.Value,
                user.Email,
                plan,
                existingSubscription?.ExternalCustomerId),
            cancellationToken).ConfigureAwait(false);
        if (sessionResult.IsFailure) {
            return Result.Failure<BillingCheckoutSessionModel>(sessionResult.Error);
        }

        var session = sessionResult.Value;

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

        return Result.Success(session);
    }

    private IBillingProviderGateway? ResolveBillingProvider(string? provider) {
        var normalizedProvider = provider?.Trim();
        return string.IsNullOrWhiteSpace(normalizedProvider)
            ? billingProviderGatewayAccessor.GetActiveProvider()
            : billingProviderGatewayAccessor.GetProviderOrDefault(normalizedProvider);
    }

    private static bool IsPaidPremiumActive(BillingSubscription? subscription, DateTime nowUtc) {
        if (subscription is null || string.IsNullOrWhiteSpace(subscription.Status)) {
            return false;
        }

        return subscription.Status.Trim().ToLowerInvariant() switch {
            "trialing" => subscription.CurrentPeriodEndUtc.HasValue && subscription.CurrentPeriodEndUtc > nowUtc,
            "active" => true,
            "past_due" => !subscription.CurrentPeriodEndUtc.HasValue || subscription.CurrentPeriodEndUtc > nowUtc,
            _ => false
        };
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
            null,
            null,
            session.PriceId,
            session.Plan,
            BillingSubscription.PendingCheckoutStatus,
            BillingPaymentKinds.Checkout,
            null,
            null,
            null,
            null,
            null,
            null);
        await billingPaymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
    }
}
