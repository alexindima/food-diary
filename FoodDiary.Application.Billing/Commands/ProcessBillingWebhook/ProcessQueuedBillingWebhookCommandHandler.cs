using FoodDiary.Application.Billing.Common;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class ProcessQueuedBillingWebhookCommandHandler(
    IBillingWebhookInboxService billingWebhookInboxService)
    : IRequestHandler<ProcessQueuedBillingWebhookCommand, Result> {
    public async Task<Result> Handle(
        ProcessQueuedBillingWebhookCommand command,
        CancellationToken cancellationToken) {
        return await billingWebhookInboxService.ProcessAsync(command.WebhookEventId, cancellationToken).ConfigureAwait(false);
    }
}
