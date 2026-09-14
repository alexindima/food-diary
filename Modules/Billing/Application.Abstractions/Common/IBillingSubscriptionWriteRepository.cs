using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingSubscriptionWriteRepository {
    Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<BillingSubscription?> GetByExternalCustomerIdAsync(string provider, string externalCustomerId, CancellationToken cancellationToken = default);

    Task<BillingSubscription?> GetByExternalSubscriptionIdAsync(string provider, string externalSubscriptionId, CancellationToken cancellationToken = default);

    Task<BillingSubscription?> GetByExternalPaymentMethodIdAsync(
        string provider,
        string externalPaymentMethodId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BillingSubscription>> GetDueForRenewalAsync(
        string provider,
        DateTime dueAtUtc,
        int limit,
        CancellationToken cancellationToken = default);

    Task<BillingSubscription> AddAsync(BillingSubscription subscription, CancellationToken cancellationToken = default);

    Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default);
}
