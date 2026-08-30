namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingWebhookEventReadRepository {
    Task<bool> ExistsAsync(string provider, string eventId, CancellationToken cancellationToken = default);
}
