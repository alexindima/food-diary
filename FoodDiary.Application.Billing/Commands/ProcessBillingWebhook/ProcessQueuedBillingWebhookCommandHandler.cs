using FoodDiary.Application.Billing.Common;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed class ProcessQueuedBillingWebhookCommandHandler(
    IBillingWebhookInboxService billingWebhookInboxService)
    : IRequestHandler<ProcessQueuedBillingWebhookCommand, Result> {
    public async Task<Result> Handle(
        ProcessQueuedBillingWebhookCommand request,
        CancellationToken cancellationToken) {
        return await billingWebhookInboxService.ProcessAsync(request.WebhookEventId, cancellationToken).ConfigureAwait(false);
    }
}
