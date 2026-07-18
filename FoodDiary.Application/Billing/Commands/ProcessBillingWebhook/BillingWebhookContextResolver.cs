using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using User = FoodDiary.Domain.Entities.Users.User;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookContextResolver(
    IBillingSubscriptionWriteRepository billingSubscriptionRepository,
    IBillingUserContextService billingUserContextService) {
    public async Task<Result<BillingWebhookProcessingContext?>> ResolveAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        CancellationToken cancellationToken) {
        BillingSubscription? subscription = await ResolveSubscriptionAsync(
            provider,
            webhookEvent,
            cancellationToken).ConfigureAwait(false);
        if (subscription is not null &&
            string.Equals(subscription.LastWebhookEventId, webhookEvent.EventId, StringComparison.Ordinal)) {
            return Result.Success<BillingWebhookProcessingContext?>(value: null);
        }

        if (subscription?.LastWebhookOccurredAtUtc is DateTime lastOccurredAtUtc &&
            webhookEvent.OccurredAtUtc is DateTime occurredAtUtc &&
            occurredAtUtc <= lastOccurredAtUtc) {
            return Result.Success<BillingWebhookProcessingContext?>(value: null);
        }

        User? user = await ResolveUserAsync(subscription, webhookEvent.UserId, cancellationToken).ConfigureAwait(false);
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
            return await billingUserContextService.GetUserIncludingDeletedAsync(subscription.UserId, cancellationToken).ConfigureAwait(false);
        }

        if (!webhookUserId.HasValue) {
            return null;
        }

        Result<UserId> webhookUserIdResult = UserIdParser.Parse(
            webhookUserId.Value,
            Errors.Billing.WebhookValidationFailed("Webhook user could not be resolved."));
        if (webhookUserIdResult.IsFailure) {
            return null;
        }

        return await billingUserContextService.GetUserIncludingDeletedAsync(webhookUserIdResult.Value, cancellationToken).ConfigureAwait(false);
    }
}
