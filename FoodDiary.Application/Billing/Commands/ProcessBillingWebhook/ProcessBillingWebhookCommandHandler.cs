using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using User = FoodDiary.Domain.Entities.Users.User;
using FoodDiary.Application.Abstractions.Billing.Models;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class ProcessBillingWebhookCommandHandler(
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingPaymentRepository billingPaymentRepository,
    IBillingWebhookEventRepository billingWebhookEventRepository,
    IUserRepository userRepository,
    BillingAccessService billingAccessService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ProcessBillingWebhookCommand, Result> {
    public async Task<Result> Handle(ProcessBillingWebhookCommand command, CancellationToken cancellationToken) {
        var billingProvider = billingProviderGatewayAccessor.GetProviderOrDefault(command.Provider);
        if (billingProvider is null) {
            return Result.Failure(Errors.Billing.InvalidProvider(command.Provider));
        }

        var webhookResult = await billingProvider.ParseWebhookEventAsync(
            command.Payload,
            command.SignatureHeader,
            cancellationToken);
        if (webhookResult.IsFailure) {
            return Result.Failure(webhookResult.Error);
        }

        var webhookEvent = webhookResult.Value;
        if (webhookEvent is null) {
            return Result.Success();
        }

        if (await billingWebhookEventRepository.ExistsAsync(billingProvider.Provider, webhookEvent.EventId, cancellationToken)) {
            return Result.Success();
        }

        var subscription = await ResolveSubscriptionAsync(
            billingProvider.Provider,
            webhookEvent,
            cancellationToken);
        if (subscription is not null &&
            string.Equals(subscription.LastWebhookEventId, webhookEvent.EventId, StringComparison.Ordinal)) {
            return Result.Success();
        }

        var user = await ResolveUserAsync(subscription, webhookEvent.UserId, cancellationToken);
        if (user is null) {
            return Result.Success();
        }

        if (subscription is null) {
            subscription = BillingSubscription.CreatePending(
                user.Id,
                billingProvider.Provider,
                webhookEvent.ExternalCustomerId,
                webhookEvent.ExternalPriceId,
                webhookEvent.Plan);
            subscription.ApplyProviderSnapshot(
                billingProvider.Provider,
                webhookEvent.ExternalSubscriptionId,
                webhookEvent.ExternalPaymentMethodId,
                webhookEvent.ExternalPriceId,
                webhookEvent.Plan,
                webhookEvent.Status,
                webhookEvent.CurrentPeriodStartUtc,
                webhookEvent.CurrentPeriodEndUtc,
                webhookEvent.CancelAtPeriodEnd,
                webhookEvent.CanceledAtUtc,
                webhookEvent.TrialStartUtc,
                webhookEvent.TrialEndUtc,
                webhookEvent.EventId,
                dateTimeProvider.UtcNow,
                webhookEvent.ProviderMetadataJson);
            await billingSubscriptionRepository.AddAsync(subscription, cancellationToken);
        } else {
            subscription.ApplyProviderSnapshot(
                billingProvider.Provider,
                webhookEvent.ExternalSubscriptionId,
                webhookEvent.ExternalPaymentMethodId,
                webhookEvent.ExternalPriceId,
                webhookEvent.Plan,
                webhookEvent.Status,
                webhookEvent.CurrentPeriodStartUtc,
                webhookEvent.CurrentPeriodEndUtc,
                webhookEvent.CancelAtPeriodEnd,
                webhookEvent.CanceledAtUtc,
                webhookEvent.TrialStartUtc,
                webhookEvent.TrialEndUtc,
                webhookEvent.EventId,
                dateTimeProvider.UtcNow,
                webhookEvent.ProviderMetadataJson);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken);
        }

        var payment = BillingPayment.Create(
            subscription.UserId,
            subscription.Id,
            billingProvider.Provider,
            webhookEvent.ExternalSubscriptionId ?? webhookEvent.ExternalPaymentMethodId ?? webhookEvent.EventId,
            webhookEvent.ExternalCustomerId,
            webhookEvent.ExternalSubscriptionId,
            webhookEvent.ExternalPaymentMethodId,
            webhookEvent.ExternalPriceId,
            webhookEvent.Plan,
            webhookEvent.Status,
            BillingPaymentKinds.Webhook,
            webhookEvent.Amount,
            webhookEvent.Currency,
            webhookEvent.CurrentPeriodStartUtc,
            webhookEvent.CurrentPeriodEndUtc,
            webhookEvent.EventId,
            webhookEvent.ProviderMetadataJson);
        await billingPaymentRepository.AddAsync(payment, cancellationToken);

        var processedWebhookEvent = BillingWebhookEvent.CreateProcessed(
            billingProvider.Provider,
            webhookEvent.EventId,
            webhookEvent.EventType,
            webhookEvent.ExternalSubscriptionId ?? webhookEvent.ExternalPaymentMethodId,
            dateTimeProvider.UtcNow,
            command.Payload);
        await billingWebhookEventRepository.AddAsync(processedWebhookEvent, cancellationToken);

        var shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
            webhookEvent.Status,
            webhookEvent.CurrentPeriodEndUtc);
        await billingAccessService.EnsurePremiumRoleAsync(user, shouldHavePremium, cancellationToken);

        return Result.Success();
    }

    private async Task<BillingSubscription?> ResolveSubscriptionAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(webhookEvent.ExternalSubscriptionId)) {
            var bySubscription = await billingSubscriptionRepository.GetByExternalSubscriptionIdAsync(
                provider,
                webhookEvent.ExternalSubscriptionId,
                cancellationToken);
            if (bySubscription is not null) {
                return bySubscription;
            }
        }

        if (!string.IsNullOrWhiteSpace(webhookEvent.ExternalPaymentMethodId)) {
            var byPaymentMethod = await billingSubscriptionRepository.GetByExternalPaymentMethodIdAsync(
                provider,
                webhookEvent.ExternalPaymentMethodId,
                cancellationToken);
            if (byPaymentMethod is not null) {
                return byPaymentMethod;
            }
        }

        if (!string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId)) {
            return await billingSubscriptionRepository.GetByExternalCustomerIdAsync(
                provider,
                webhookEvent.ExternalCustomerId,
                cancellationToken);
        }

        return null;
    }

    private async Task<User?> ResolveUserAsync(
        BillingSubscription? subscription,
        Guid? webhookUserId,
        CancellationToken cancellationToken) {
        if (subscription is not null) {
            return await userRepository.GetByIdAsync(subscription.UserId, cancellationToken);
        }

        if (!webhookUserId.HasValue || webhookUserId == Guid.Empty) {
            return null;
        }

        return await userRepository.GetByIdAsync(new UserId(webhookUserId.Value), cancellationToken);
    }
}
