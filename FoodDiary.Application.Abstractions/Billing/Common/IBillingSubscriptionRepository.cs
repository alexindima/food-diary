using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingSubscriptionRepository {
    Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<BillingSubscription?> GetByExternalCustomerIdAsync(string provider, string externalCustomerId, CancellationToken cancellationToken = default);
    Task<BillingSubscription?> GetByExternalSubscriptionIdAsync(string provider, string externalSubscriptionId, CancellationToken cancellationToken = default);
    Task<BillingSubscription> AddAsync(BillingSubscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default);
}
