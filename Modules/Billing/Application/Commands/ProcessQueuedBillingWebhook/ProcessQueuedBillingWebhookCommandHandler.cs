using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using System.Text.Json;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessQueuedBillingWebhook;

public sealed class ProcessQueuedBillingWebhookCommandHandler(
    IBillingWebhookEventWriteRepository events,
    IBillingTransactionRunner transactions,
    BillingWebhookEventProcessor processor,
    TimeProvider timeProvider,
    BillingWebhookContextResolver contextResolver) : IRequestHandler<ProcessQueuedBillingWebhookCommand, Result> {
    public async Task<Result> Handle(ProcessQueuedBillingWebhookCommand request, CancellationToken cancellationToken) {
        var eventId = (BillingWebhookEventId)request.WebhookEventId;
        BillingWebhookEvent? inboxEvent = await events.GetByIdAsync(eventId, cancellationToken).ConfigureAwait(false);
        if (inboxEvent is null || inboxEvent.ProcessingState == BillingWebhookProcessingState.Processed) {
            return Result.Success();
        }

        Result result;
        string serializationKey = $"billing-inbox:{inboxEvent.Id:N}";
        try {
            BillingWebhookEventModel? webhookEvent = JsonSerializer.Deserialize<BillingWebhookEventModel>(inboxEvent.ParsedEventJson ?? string.Empty);
            if (webhookEvent is null) {
                result = Result.Failure(BillingErrors.WebhookValidationFailed("Stored webhook event is empty."));
            } else {
                serializationKey = await contextResolver.GetSerializationKeyAsync(inboxEvent.Provider, webhookEvent, cancellationToken).ConfigureAwait(false);
                result = await processor.ProcessAsync(inboxEvent.Provider, inboxEvent.PayloadJson ?? "{}", webhookEvent, inboxEvent, cancellationToken).ConfigureAwait(false);
            }
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception) when (!cancellationToken.IsCancellationRequested) {
            // Do not persist exception messages: provider payloads and identifiers may be sensitive.
            result = Result.Failure(BillingErrors.WebhookProcessingFailed);
        }

        if (result.IsSuccess) {
            return result;
        }

        await transactions.ExecuteSerializedAsync(serializationKey, async ct => {
            inboxEvent = await events.GetByIdAsync(eventId, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The webhook inbox event no longer exists.");
            if (inboxEvent.ProcessingState == BillingWebhookProcessingState.Processed) {
                result = Result.Success();
                return;
            }
            inboxEvent.MarkFailed(timeProvider.GetUtcNow().UtcDateTime, result.Error.Message);
            await events.UpdateAsync(inboxEvent, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return result;
    }
}
