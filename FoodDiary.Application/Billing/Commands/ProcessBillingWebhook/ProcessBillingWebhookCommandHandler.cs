using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Application.Abstractions.Billing.Models;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class ProcessBillingWebhookCommandHandler(
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor,
    IBillingWebhookEventWriteRepository billingWebhookEventRepository,
    IBillingTransactionRunner billingTransactionRunner,
    BillingWebhookContextResolver billingWebhookContextResolver,
    BillingWebhookSubscriptionWriter billingWebhookSubscriptionWriter,
    BillingWebhookPaymentRecorder billingWebhookPaymentRecorder,
    BillingWebhookPremiumRoleSyncer billingWebhookPremiumRoleSyncer)
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

        Result<BillingWebhookProcessingContext?> contextResult = await billingWebhookContextResolver.ResolveAsync(
            billingProvider.Provider,
            webhookEvent,
            cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure(contextResult.Error);
        }

        BillingWebhookProcessingContext? context = contextResult.Value;
        if (context is null) {
            return Result.Success();
        }

        try {
            await ProcessWebhookEventAsync(
                command.Payload,
                billingProvider.Provider,
                webhookEvent,
                context,
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
        BillingWebhookProcessingContext context,
        CancellationToken cancellationToken) {
        await billingTransactionRunner.ExecuteAsync(async ct => {
            BillingWebhookEvent processedWebhookEvent = billingWebhookSubscriptionWriter.CreateProcessedEvent(provider, webhookEvent, payload);
            await billingWebhookEventRepository.AddAsync(processedWebhookEvent, ct).ConfigureAwait(false);

            BillingSubscription updatedSubscription = await billingWebhookSubscriptionWriter.UpsertAsync(
                provider,
                webhookEvent,
                context.Subscription,
                context.User,
                ct).ConfigureAwait(false);

            await billingWebhookPaymentRecorder.AddIfPresentAsync(updatedSubscription, provider, webhookEvent, ct).ConfigureAwait(false);

            await billingWebhookPremiumRoleSyncer.SyncAsync(context.User, updatedSubscription, webhookEvent, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }
}
