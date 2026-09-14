namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingWebhookEventReadRepository {
    Task<bool> ExistsAsync(string provider, string eventId, CancellationToken cancellationToken = default);
}
