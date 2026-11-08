using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingPaymentWriteRepository {
    Task<BillingPayment?> GetLatestCheckoutAsync(UserId userId, BillingSubscriptionId subscriptionId,
        string provider, CancellationToken cancellationToken = default);

    Task<BillingPayment?> GetByExternalPaymentIdAsync(
        string provider,
        string externalPaymentId,
        CancellationToken cancellationToken = default);

    Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default);

    Task UpdateAsync(BillingPayment payment, CancellationToken cancellationToken = default);
}
