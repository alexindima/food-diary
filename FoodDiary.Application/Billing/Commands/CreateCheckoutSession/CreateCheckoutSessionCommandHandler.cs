using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Commands.CreateCheckoutSession;

public sealed class CreateCheckoutSessionCommandHandler(
    IUserRepository userRepository,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingPaymentRepository billingPaymentRepository,
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor)
    : ICommandHandler<CreateCheckoutSessionCommand, Result<BillingCheckoutSessionModel>> {
    public async Task<Result<BillingCheckoutSessionModel>> Handle(
        CreateCheckoutSessionCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<BillingCheckoutSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<BillingCheckoutSessionModel>(accessError);
        }

        if (user!.HasRole(RoleNames.Premium)) {
            return Result.Failure<BillingCheckoutSessionModel>(Errors.Billing.SubscriptionAlreadyActive);
        }

        var existingSubscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken);
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
            cancellationToken);
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
            await billingSubscriptionRepository.AddAsync(pendingSubscription, cancellationToken);
            await AddCheckoutPaymentAsync(pendingSubscription, billingProvider.Provider, session, cancellationToken);
        } else {
            existingSubscription.UpdateCheckoutContext(
                billingProvider.Provider,
                session.CustomerId,
                session.PriceId,
                session.Plan);
            await billingSubscriptionRepository.UpdateAsync(existingSubscription, cancellationToken);
            await AddCheckoutPaymentAsync(existingSubscription, billingProvider.Provider, session, cancellationToken);
        }

        return Result.Success(session);
    }

    private IBillingProviderGateway? ResolveBillingProvider(string? provider) =>
        string.IsNullOrWhiteSpace(provider)
            ? billingProviderGatewayAccessor.GetActiveProvider()
            : billingProviderGatewayAccessor.GetProviderOrDefault(provider);

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
        await billingPaymentRepository.AddAsync(payment, cancellationToken);
    }
}
