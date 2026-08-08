using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class ProcessBillingWebhookCommandHandler(
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor,
    IBillingWebhookEventWriteRepository billingWebhookEventRepository,
    IBillingTransactionRunner billingTransactionRunner,
    BillingWebhookEventProcessor processor,
    TimeProvider timeProvider)
    : IRequestHandler<ProcessBillingWebhookCommand, Result> {
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

        Error? validationError = BillingWebhookEventValidator.Validate(billingProvider.Provider, webhookEvent);
        if (validationError is not null) {
            return Result.Failure(validationError);
        }

        if (await billingWebhookEventRepository.ExistsAsync(
            billingProvider.Provider,
            webhookEvent.EventId,
            cancellationToken).ConfigureAwait(false)) {
            return Result.Success();
        }

        if (!command.QueueOnly) {
            return await processor.ProcessAsync(
                billingProvider.Provider,
                command.Payload,
                webhookEvent,
                inboxEvent: null,
                cancellationToken).ConfigureAwait(false);
        }

        var inboxEvent = BillingWebhookEvent.CreateReceived(
            billingProvider.Provider,
            webhookEvent.EventId,
            webhookEvent.EventType,
            webhookEvent.ExternalPaymentId ?? webhookEvent.ExternalSubscriptionId ?? webhookEvent.ExternalPaymentMethodId,
            timeProvider.GetUtcNow().UtcDateTime,
            command.Payload,
            JsonSerializer.Serialize(webhookEvent));
        try {
            await billingTransactionRunner.ExecuteAsync(
                ct => billingWebhookEventRepository.AddAsync(inboxEvent, ct),
                cancellationToken).ConfigureAwait(false);
        } catch (BillingWebhookEventAlreadyProcessedException) {
            return Result.Success();
        }

        return Result.Success();
    }
}
