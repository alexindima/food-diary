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
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class ProcessBillingWebhookCommandHandler(
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingPaymentRepository billingPaymentRepository,
    IBillingWebhookEventRepository billingWebhookEventRepository,
    IBillingTransactionRunner billingTransactionRunner,
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
            cancellationToken).ConfigureAwait(false);
        if (webhookResult.IsFailure) {
            return Result.Failure(webhookResult.Error);
        }

        var webhookEvent = webhookResult.Value;
        if (webhookEvent is null) {
            return Result.Success();
        }

        var webhookEventValidationError = ValidateWebhookEvent(webhookEvent);
        if (webhookEventValidationError is not null) {
            return Result.Failure(webhookEventValidationError);
        }

        if (await billingWebhookEventRepository.ExistsAsync(billingProvider.Provider, webhookEvent.EventId, cancellationToken).ConfigureAwait(false)) {
            return Result.Success();
        }

        var subscription = await ResolveSubscriptionAsync(
            billingProvider.Provider,
            webhookEvent,
            cancellationToken).ConfigureAwait(false);
        if (subscription is not null &&
            string.Equals(subscription.LastWebhookEventId, webhookEvent.EventId, StringComparison.Ordinal)) {
            return Result.Success();
        }

        var user = await ResolveUserAsync(subscription, webhookEvent.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure(Errors.Billing.WebhookValidationFailed("Webhook user could not be resolved."));
        }

        try {
            await ProcessWebhookEventAsync(
                command.Payload,
                billingProvider.Provider,
                webhookEvent,
                subscription,
                user,
                cancellationToken).ConfigureAwait(false);
        } catch (BillingWebhookEventAlreadyProcessedException) {
            return Result.Success();
        } catch (BillingPaymentAlreadyExistsException) {
            return Result.Success();
        }

        return Result.Success();
    }

    private async Task ProcessWebhookEventAsync(
        string payload,
        string provider,
        BillingWebhookEventModel webhookEvent,
        BillingSubscription? subscription,
        User user,
        CancellationToken cancellationToken) {
        await billingTransactionRunner.ExecuteAsync(async ct => {
            var processedWebhookEvent = CreateProcessedWebhookEvent(provider, webhookEvent, payload);
            await billingWebhookEventRepository.AddAsync(processedWebhookEvent, ct).ConfigureAwait(false);

            var updatedSubscription = await UpsertSubscriptionAsync(
                provider,
                webhookEvent,
                subscription,
                user,
                ct).ConfigureAwait(false);

            await AddWebhookPaymentIfPresentAsync(updatedSubscription, provider, webhookEvent, ct).ConfigureAwait(false);

            var shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
                webhookEvent.Status,
                webhookEvent.CurrentPeriodEndUtc);
            await SyncPremiumRoleAsync(user, updatedSubscription, shouldHavePremium, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private BillingWebhookEvent CreateProcessedWebhookEvent(
        string provider,
        BillingWebhookEventModel webhookEvent,
        string payload) =>
        BillingWebhookEvent.CreateProcessed(
            provider,
            webhookEvent.EventId,
            webhookEvent.EventType,
            webhookEvent.ExternalSubscriptionId ?? webhookEvent.ExternalPaymentMethodId,
            dateTimeProvider.UtcNow,
            payload);

    private async Task<BillingSubscription> UpsertSubscriptionAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        BillingSubscription? subscription,
        User user,
        CancellationToken cancellationToken) {
        var currentSubscription = subscription ?? BillingSubscription.CreatePending(
            user.Id,
            provider,
            webhookEvent.ExternalCustomerId,
            webhookEvent.ExternalPriceId,
            webhookEvent.Plan);

        ApplyWebhookSnapshot(currentSubscription, provider, webhookEvent);

        if (subscription is null) {
            await billingSubscriptionRepository.AddAsync(currentSubscription, cancellationToken).ConfigureAwait(false);
        } else {
            await billingSubscriptionRepository.UpdateAsync(currentSubscription, cancellationToken).ConfigureAwait(false);
        }

        return currentSubscription;
    }

    private void ApplyWebhookSnapshot(
        BillingSubscription subscription,
        string provider,
        BillingWebhookEventModel webhookEvent) {
        subscription.ApplyProviderSnapshot(
            provider,
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
    }

    private static Error? ValidateWebhookEvent(BillingWebhookEventModel webhookEvent) {
        if (string.IsNullOrWhiteSpace(webhookEvent.EventId)) {
            return Errors.Billing.WebhookValidationFailed("Webhook event id is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.EventType)) {
            return Errors.Billing.WebhookValidationFailed("Webhook event type is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId)) {
            return Errors.Billing.WebhookValidationFailed("Webhook customer id is required.");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.Status)) {
            return Errors.Billing.WebhookValidationFailed("Webhook subscription status is required.");
        }

        return null;
    }

    private async Task AddWebhookPaymentIfPresentAsync(
        BillingSubscription subscription,
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        if (!webhookEvent.Amount.HasValue) {
            return;
        }

        var externalPaymentId = webhookEvent.ExternalSubscriptionId ??
            webhookEvent.ExternalPaymentMethodId ??
            webhookEvent.EventId;
        var existingPayment = await billingPaymentRepository.GetByExternalPaymentIdAsync(
            provider,
            externalPaymentId,
            cancellationToken).ConfigureAwait(false);
        if (existingPayment is not null) {
            return;
        }

        var payment = BillingPayment.Create(
            subscription.UserId,
            subscription.Id,
            provider,
            externalPaymentId,
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
        await billingPaymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
    }

    private async Task<BillingSubscription?> ResolveSubscriptionAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(webhookEvent.ExternalSubscriptionId)) {
            var bySubscription = await billingSubscriptionRepository.GetByExternalSubscriptionIdAsync(
                provider,
                webhookEvent.ExternalSubscriptionId,
                cancellationToken).ConfigureAwait(false);
            if (bySubscription is not null) {
                return bySubscription;
            }
        }

        if (!string.IsNullOrWhiteSpace(webhookEvent.ExternalPaymentMethodId)) {
            var byPaymentMethod = await billingSubscriptionRepository.GetByExternalPaymentMethodIdAsync(
                provider,
                webhookEvent.ExternalPaymentMethodId,
                cancellationToken).ConfigureAwait(false);
            if (byPaymentMethod is not null) {
                return byPaymentMethod;
            }
        }

        if (!string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId)) {
            return await billingSubscriptionRepository.GetByExternalCustomerIdAsync(
                provider,
                webhookEvent.ExternalCustomerId,
                cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private async Task<User?> ResolveUserAsync(
        BillingSubscription? subscription,
        Guid? webhookUserId,
        CancellationToken cancellationToken) {
        if (subscription is not null) {
            return await userRepository.GetByIdIncludingDeletedAsync(subscription.UserId, cancellationToken).ConfigureAwait(false);
        }

        if (!webhookUserId.HasValue || webhookUserId == Guid.Empty) {
            return null;
        }

        return await userRepository.GetByIdIncludingDeletedAsync(new UserId(webhookUserId.Value), cancellationToken).ConfigureAwait(false);
    }

    private async Task SyncPremiumRoleAsync(
        User user,
        BillingSubscription subscription,
        bool shouldHavePremium,
        CancellationToken cancellationToken) {
        var canAccess = CurrentUserAccessPolicy.EnsureCanAccess(user) is null;
        if (canAccess) {
            await billingAccessService.EnsurePremiumRoleAsync(user, subscription, shouldHavePremium, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (subscription.PremiumRoleManagedByBilling) {
            subscription.MarkPremiumRoleManagedByBilling(false, dateTimeProvider.UtcNow);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
    }
}
