using FoodDiary.Application.Billing.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingWebhookInboxService {
    Task<Result> ProcessAsync(Guid webhookEventId, CancellationToken cancellationToken = default);

    Task<BillingWebhookInboxRunResult> ProcessPendingAsync(int batchSize, CancellationToken cancellationToken = default);
}
