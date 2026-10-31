using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.Enums;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;

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
        string serializationKey = await billingWebhookContextResolver.GetSerializationKeyAsync(provider, webhookEvent, cancellationToken);
        try {
            await billingTransactionRunner.ExecuteSerializedAsync(serializationKey, async ct => {
                processingResult = Result.Success();
                if (inboxEvent is not null) {
                    inboxEvent = await billingWebhookEventRepository.GetByIdAsync(inboxEvent.TypedId, ct)
                        ?? throw new InvalidOperationException("The webhook inbox event no longer exists.");
                    if (inboxEvent.ProcessingState == BillingWebhookProcessingState.Processed) {
                        return;
                    }
                }
                Result<BillingWebhookProcessingContext?> contextResult = await billingWebhookContextResolver.ResolveAsync(
                    provider,
                    webhookEvent,
                    ct);
                if (contextResult.IsFailure) {
                    processingResult = Result.Failure(contextResult.Error);
                    return;
                }

                BillingWebhookEvent persistedEvent = inboxEvent ??
                    billingWebhookSubscriptionWriter.CreateProcessedEvent(provider, webhookEvent, payload);
                if (inboxEvent is null) {
                    await billingWebhookEventRepository.AddAsync(persistedEvent, ct);
                }

                BillingWebhookProcessingContext? context = contextResult.Value;
                if (context is not null) {
                    await ApplyBusinessEffectsAsync(provider, context.EffectiveEvent, context, ct);
                }

                if (inboxEvent is not null) {
                    inboxEvent.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                    await billingWebhookEventRepository.UpdateAsync(inboxEvent, ct);
                }
            }, cancellationToken);
        } catch (BillingWebhookEventAlreadyProcessedException) {
            return Result.Success();
        } catch (BillingPaymentAlreadyExistsException) {
            if (inboxEvent is null) {
                return Result.Success();
            }

            await billingTransactionRunner.ExecuteAsync(async ct => {
                inboxEvent = await billingWebhookEventRepository.GetByIdAsync(inboxEvent.TypedId, ct)
                    ?? throw new InvalidOperationException("The webhook inbox event no longer exists.");
                inboxEvent.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                await billingWebhookEventRepository.UpdateAsync(inboxEvent, ct);
            },
                cancellationToken);

            return Result.Success();
        }

        return processingResult;
    }

    private async Task ApplyBusinessEffectsAsync(
        string provider,
        BillingWebhookEventModel webhookEvent,
        BillingWebhookProcessingContext context,
        CancellationToken cancellationToken) {
        BillingSubscription? updatedSubscription = context.Subscription;
        if (context.ShouldUpdateSubscription) {
            updatedSubscription = await billingWebhookSubscriptionWriter.UpsertAsync(
                provider,
                webhookEvent,
                context.Subscription,
                context.User,
                cancellationToken);

            await billingWebhookPremiumRoleSyncer.SyncAsync(
                context.User,
                updatedSubscription,
                webhookEvent,
                cancellationToken);
        }

        await billingWebhookPaymentRecorder.AddIfPresentAsync(
            updatedSubscription,
            context.User.UserId,
            provider,
            webhookEvent,
            cancellationToken);
    }
}
