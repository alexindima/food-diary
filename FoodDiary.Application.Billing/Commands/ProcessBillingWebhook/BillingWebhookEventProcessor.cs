using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class BillingWebhookEventProcessor(
    IBillingWebhookEventWriteRepository billingWebhookEventRepository,
    IBillingTransactionRunner billingTransactionRunner,
    BillingWebhookContextResolver billingWebhookContextResolver,
    BillingWebhookSubscriptionWriter billingWebhookSubscriptionWriter,
    BillingWebhookPaymentRecorder billingWebhookPaymentRecorder,
    BillingWebhookPremiumRoleSyncer billingWebhookPremiumRoleSyncer,
    TimeProvider timeProvider) {
    public async Task<Result> ProcessAsync(
        string provider,
        string payload,
        BillingWebhookEventModel webhookEvent,
        BillingWebhookEvent? inboxEvent,
        CancellationToken cancellationToken) {
        var processingResult = Result.Success();
        try {
            await billingTransactionRunner.ExecuteSerializedAsync(CreateSerializationKey(provider, webhookEvent), async ct => {
                Result<BillingWebhookProcessingContext?> contextResult = await billingWebhookContextResolver.ResolveAsync(
                    provider,
                    webhookEvent,
                    ct).ConfigureAwait(false);
                if (contextResult.IsFailure) {
                    processingResult = Result.Failure(contextResult.Error);
                    return;
                }

                BillingWebhookEvent persistedEvent = inboxEvent ??
                    billingWebhookSubscriptionWriter.CreateProcessedEvent(provider, webhookEvent, payload);
                if (inboxEvent is null) {
                    await billingWebhookEventRepository.AddAsync(persistedEvent, ct).ConfigureAwait(false);
                }

                BillingWebhookProcessingContext? context = contextResult.Value;
                if (context is not null) {
                    await ApplyBusinessEffectsAsync(provider, webhookEvent, context, ct).ConfigureAwait(false);
                }

                if (inboxEvent is not null) {
                    inboxEvent.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                    await billingWebhookEventRepository.UpdateAsync(inboxEvent, ct).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);
        } catch (BillingWebhookEventAlreadyProcessedException) {
            return Result.Success();
        } catch (BillingPaymentAlreadyExistsException) {
            if (inboxEvent is null) {
                return Result.Success();
            }

            inboxEvent.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
            await billingWebhookEventRepository.UpdateAsync(inboxEvent, cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }

        return processingResult;
    }

    private static string CreateSerializationKey(string provider, BillingWebhookEventModel webhookEvent) {
        string externalObjectId = webhookEvent.ExternalPaymentMethodId
            ?? webhookEvent.ExternalSubscriptionId
            ?? webhookEvent.ExternalCustomerId
            ?? webhookEvent.RelatedTransactionId
            ?? webhookEvent.EventId;
        return $"billing-webhook:{provider.Trim().ToLowerInvariant()}:{externalObjectId}";
    }

    private async Task ApplyBusinessEffectsAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        BillingWebhookProcessingContext context,
        CancellationToken cancellationToken) {
        BillingSubscription? updatedSubscription = context.Subscription;
        if (webhookEvent.UpdatesSubscription) {
            updatedSubscription = await billingWebhookSubscriptionWriter.UpsertAsync(
                provider,
                webhookEvent,
                context.Subscription,
                context.User,
                cancellationToken).ConfigureAwait(false);

            await billingWebhookPremiumRoleSyncer.SyncAsync(
                context.User,
                updatedSubscription,
                webhookEvent,
                cancellationToken).ConfigureAwait(false);
        }

        await billingWebhookPaymentRecorder.AddIfPresentAsync(
            updatedSubscription,
            context.User.UserId,
            provider,
            webhookEvent,
            cancellationToken).ConfigureAwait(false);
    }
}
