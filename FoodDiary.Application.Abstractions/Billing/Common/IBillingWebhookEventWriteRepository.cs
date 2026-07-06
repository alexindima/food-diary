using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingWebhookEventWriteRepository {
    Task<bool> ExistsAsync(string provider, string eventId, CancellationToken cancellationToken = default);

    Task<BillingWebhookEvent> AddAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
