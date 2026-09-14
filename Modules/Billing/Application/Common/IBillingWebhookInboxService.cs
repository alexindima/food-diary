using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Common;

public interface IBillingWebhookInboxService {
    Task<Result> ProcessAsync(Guid webhookEventId, CancellationToken cancellationToken = default);

    Task<BillingWebhookInboxRunResult> ProcessPendingAsync(int batchSize, CancellationToken cancellationToken = default);
}
