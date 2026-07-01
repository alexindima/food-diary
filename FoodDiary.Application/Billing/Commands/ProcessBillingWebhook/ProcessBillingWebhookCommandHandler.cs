using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using User = FoodDiary.Domain.Entities.Users.User;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class ProcessBillingWebhookCommandHandler(
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingWebhookEventRepository billingWebhookEventRepository,
    IBillingTransactionRunner billingTransactionRunner,
    IUserRepository userRepository,
    BillingAccessService billingAccessService,
    BillingWebhookPaymentRecorder billingWebhookPaymentRecorder,
    TimeProvider dateTimeProvider)
    : ICommandHandler<ProcessBillingWebhookCommand, Result> {
    public async Task<Result> Handle(ProcessBillingWebhookCommand command, CancellationToken cancellationToken) {
        IBillingProviderGateway? billingProvider = billingProviderGatewayAccessor.GetProviderOrDefault(command.Provider);
        if (billingProvider is null) {
            return Result.Failure(Errors.Billing.InvalidProvider(command.Provider));
        }

        Result<BillingWebhookEventModel?> webhookResult = await billingProvider.ParseWebhookEventAsync(
            command.Payload,
            command.SignatureHeader,
            cancellationToken).ConfigureAwait(false);
        if (webhookResult.IsFailure) {
            return Result.Failure(webhookResult.Error);
        }

        BillingWebhookEventModel? webhookEvent = webhookResult.Value;
        if (webhookEvent is null) {
            return Result.Success();
        }

        Error? webhookEventValidationError = BillingWebhookEventValidator.Validate(webhookEvent);
        if (webhookEventValidationError is not null) {
            return Result.Failure(webhookEventValidationError);
        }

        if (await billingWebhookEventRepository.ExistsAsync(billingProvider.Provider, webhookEvent.EventId, cancellationToken).ConfigureAwait(false)) {
            return Result.Success();
        }

        BillingSubscription? subscription = await ResolveSubscriptionAsync(
            billingProvider.Provider,
            webhookEvent,
            cancellationToken).ConfigureAwait(false);
        if (subscription is not null &&
            string.Equals(subscription.LastWebhookEventId, webhookEvent.EventId, StringComparison.Ordinal)) {
            return Result.Success();
        }

        User? user = await ResolveUserAsync(subscription, webhookEvent.UserId, cancellationToken).ConfigureAwait(false);
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
            BillingWebhookEvent processedWebhookEvent = CreateProcessedWebhookEvent(provider, webhookEvent, payload);
            await billingWebhookEventRepository.AddAsync(processedWebhookEvent, ct).ConfigureAwait(false);

            BillingSubscription updatedSubscription = await UpsertSubscriptionAsync(
                provider,
                webhookEvent,
                subscription,
                user,
                ct).ConfigureAwait(false);

            await billingWebhookPaymentRecorder.AddIfPresentAsync(updatedSubscription, provider, webhookEvent, ct).ConfigureAwait(false);

            bool shouldHavePremium = billingAccessService.ShouldHavePremiumAccess(
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
            dateTimeProvider.GetUtcNow().UtcDateTime,
            payload);

    private async Task<BillingSubscription> UpsertSubscriptionAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        BillingSubscription? subscription,
        User user,
        CancellationToken cancellationToken) {
        BillingSubscription currentSubscription = subscription ?? BillingSubscription.CreatePending(
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
            dateTimeProvider.GetUtcNow().UtcDateTime,
            webhookEvent.ProviderMetadataJson);
    }

    private async Task<BillingSubscription?> ResolveSubscriptionAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(webhookEvent.ExternalSubscriptionId)) {
            BillingSubscription? bySubscription = await billingSubscriptionRepository.GetByExternalSubscriptionIdAsync(
                provider,
                webhookEvent.ExternalSubscriptionId,
                cancellationToken).ConfigureAwait(false);
            if (bySubscription is not null) {
                return bySubscription;
            }
        }

        if (!string.IsNullOrWhiteSpace(webhookEvent.ExternalPaymentMethodId)) {
            BillingSubscription? byPaymentMethod = await billingSubscriptionRepository.GetByExternalPaymentMethodIdAsync(
                provider,
                webhookEvent.ExternalPaymentMethodId,
                cancellationToken).ConfigureAwait(false);
            if (byPaymentMethod is not null) {
                return byPaymentMethod;
            }
        }

        return await billingSubscriptionRepository.GetByExternalCustomerIdAsync(
            provider,
            webhookEvent.ExternalCustomerId!,
            cancellationToken).ConfigureAwait(false);
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
        bool canAccess = CurrentUserAccessPolicy.EnsureCanAccess(user) is null;
        if (canAccess) {
            await billingAccessService.EnsurePremiumRoleAsync(user, subscription, shouldHavePremium, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (subscription.PremiumRoleManagedByBilling) {
            subscription.MarkPremiumRoleManagedByBilling(value: false, dateTimeProvider.GetUtcNow().UtcDateTime);
            await billingSubscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
    }
}
