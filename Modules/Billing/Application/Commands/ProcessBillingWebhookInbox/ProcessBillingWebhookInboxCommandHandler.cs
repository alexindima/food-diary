using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Commands.ProcessQueuedBillingWebhook;
using FoodDiary.Modules.Billing.Contracts.Commands.ProcessBillingWebhookInbox;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhookInbox;

public sealed class ProcessBillingWebhookInboxCommandHandler(IBillingWebhookEventWriteRepository events, ISender sender)
    : IRequestHandler<ProcessBillingWebhookInboxCommand, BillingWebhookInboxRunResult> {
    public async Task<BillingWebhookInboxRunResult> Handle(ProcessBillingWebhookInboxCommand request, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BatchSize, nameof(request));
        IReadOnlyList<BillingWebhookEvent> pending = await events.GetPendingAsync(request.BatchSize, cancellationToken).ConfigureAwait(false);
        int processed = 0;
        int failed = 0;
        foreach (BillingWebhookEvent item in pending) {
            cancellationToken.ThrowIfCancellationRequested();
            Result result = await sender.Send(new ProcessQueuedBillingWebhookCommand(item.Id), cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess) {
                processed++;
            } else {
                failed++;
            }
        }
        return new BillingWebhookInboxRunResult(processed, failed);
    }
}
