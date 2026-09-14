using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingWebhookEventWriteRepository {
    Task<bool> ExistsAsync(string provider, string eventId, CancellationToken cancellationToken = default);

    Task<BillingWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BillingWebhookEvent>> GetPendingAsync(int limit, CancellationToken cancellationToken = default);

    Task<BillingWebhookEvent> AddAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    Task UpdateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
