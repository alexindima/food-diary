using System.Text.Json;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Services;

public sealed class BillingWebhookInboxService(
    IBillingWebhookEventWriteRepository billingWebhookEventRepository,
    IBillingTransactionRunner billingTransactionRunner,
    BillingWebhookEventProcessor processor,
    TimeProvider timeProvider) : IBillingWebhookInboxService {
    public async Task<Result> ProcessAsync(Guid webhookEventId, CancellationToken cancellationToken = default) {
        BillingWebhookEvent? inboxEvent = await billingWebhookEventRepository.GetByIdAsync(
            webhookEventId,
            cancellationToken).ConfigureAwait(false);
        if (inboxEvent is null || string.Equals(inboxEvent.Status, BillingWebhookEvent.ProcessedStatus, StringComparison.Ordinal)) {
            return Result.Success();
        }

        BillingWebhookEventModel? webhookEvent;
        Error? deserializationError = null;
        try {
            webhookEvent = JsonSerializer.Deserialize<BillingWebhookEventModel>(inboxEvent.ParsedEventJson ?? string.Empty);
        } catch (JsonException ex) {
            webhookEvent = null;
            deserializationError = Errors.Billing.WebhookValidationFailed(ex.Message);
        }

        Result result;
        if (deserializationError is not null) {
            result = Result.Failure(deserializationError);
        } else if (webhookEvent is null) {
            result = Result.Failure(Errors.Billing.WebhookValidationFailed("Stored webhook event is empty."));
        } else {
            result = await processor.ProcessAsync(
                inboxEvent.Provider,
                inboxEvent.PayloadJson ?? "{}",
                webhookEvent,
                inboxEvent,
                cancellationToken).ConfigureAwait(false);
        }
        if (result.IsSuccess) {
            return result;
        }

        inboxEvent.MarkFailed(timeProvider.GetUtcNow().UtcDateTime, result.Error.Message);
        await billingTransactionRunner.ExecuteAsync(
            ct => billingWebhookEventRepository.UpdateAsync(inboxEvent, ct),
            cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<BillingWebhookInboxRunResult> ProcessPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<BillingWebhookEvent> events = await billingWebhookEventRepository
            .GetPendingAsync(batchSize, cancellationToken)
            .ConfigureAwait(false);
        int processed = 0;
        int failed = 0;
        foreach (BillingWebhookEvent webhookEvent in events) {
            Result result = await ProcessAsync(webhookEvent.Id, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure) {
                failed++;
            } else {
                processed++;
            }
        }

        return new BillingWebhookInboxRunResult(processed, failed);
    }
}
