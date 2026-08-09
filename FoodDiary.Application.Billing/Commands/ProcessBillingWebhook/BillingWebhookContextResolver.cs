using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using User = FoodDiary.Domain.Entities.Users.User;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookContextResolver(
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    IBillingUserContextService billingUserContextService,
    IBillingPaymentWriteRepository? billingPaymentRepository = null) {
    public async Task<Result<BillingWebhookProcessingContext?>> ResolveAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        BillingSubscription? subscription = await ResolveSubscriptionAsync(
            provider,
            webhookEvent,
            cancellationToken).ConfigureAwait(false);
        if (webhookEvent.UpdatesSubscription &&
            subscription is not null &&
            string.Equals(subscription.LastWebhookEventId, webhookEvent.EventId, StringComparison.Ordinal)) {
            return Result.Success<BillingWebhookProcessingContext?>(value: null);
        }

        if (webhookEvent.UpdatesSubscription &&
            subscription?.LastWebhookOccurredAtUtc is { } lastOccurredAtUtc &&
            webhookEvent.OccurredAtUtc is { } occurredAtUtc &&
            occurredAtUtc < lastOccurredAtUtc) {
            return Result.Success<BillingWebhookProcessingContext?>(value: null);
        }

        BillingPayment? relatedPayment = await ResolveRelatedPaymentAsync(
            provider,
            webhookEvent.RelatedTransactionId,
            cancellationToken).ConfigureAwait(false);
        User? user = await ResolveUserAsync(
            subscription,
            webhookEvent.UserId,
            relatedPayment?.UserId,
            cancellationToken).ConfigureAwait(false);
        return user is null
            ? Result.Failure<BillingWebhookProcessingContext?>(
                Errors.Billing.WebhookValidationFailed("Webhook user could not be resolved."))
            : Result.Success<BillingWebhookProcessingContext?>(new BillingWebhookProcessingContext(subscription, user));
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

        if (string.IsNullOrWhiteSpace(webhookEvent.ExternalPaymentMethodId)) {
            return string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId)
                ? null
                : await billingSubscriptionRepository.GetByExternalCustomerIdAsync(
                    provider,
                    webhookEvent.ExternalCustomerId,
                    cancellationToken).ConfigureAwait(false);
        }

        BillingSubscription? byPaymentMethod = await billingSubscriptionRepository.GetByExternalPaymentMethodIdAsync(
            provider,
            webhookEvent.ExternalPaymentMethodId,
            cancellationToken).ConfigureAwait(false);
        return byPaymentMethod ?? (string.IsNullOrWhiteSpace(webhookEvent.ExternalCustomerId)
            ? null
            : await billingSubscriptionRepository.GetByExternalCustomerIdAsync(
                provider,
                webhookEvent.ExternalCustomerId,
                cancellationToken).ConfigureAwait(false));
    }

    private Task<BillingPayment?> ResolveRelatedPaymentAsync(
        string provider,
        string? relatedTransactionId,
        CancellationToken cancellationToken) {
        return billingPaymentRepository is null || string.IsNullOrWhiteSpace(relatedTransactionId)
            ? Task.FromResult<BillingPayment?>(null)
            : billingPaymentRepository.GetByExternalPaymentIdAsync(provider, relatedTransactionId, cancellationToken);
    }

    private async Task<User?> ResolveUserAsync(
        BillingSubscription? subscription,
        Guid? webhookUserId,
        UserId? relatedPaymentUserId,
        CancellationToken cancellationToken) {
        if (subscription is not null) {
            return await billingUserContextService.GetUserIncludingDeletedAsync(subscription.UserId, cancellationToken).ConfigureAwait(false);
        }

        if (webhookUserId.HasValue && webhookUserId.Value != Guid.Empty) {
            return await billingUserContextService.GetUserIncludingDeletedAsync(
                new UserId(webhookUserId.Value),
                cancellationToken).ConfigureAwait(false);
        }

        return relatedPaymentUserId is null || relatedPaymentUserId == UserId.Empty
            ? null
            : await billingUserContextService.GetUserIncludingDeletedAsync(
                relatedPaymentUserId.Value,
                cancellationToken).ConfigureAwait(false);
    }
}
