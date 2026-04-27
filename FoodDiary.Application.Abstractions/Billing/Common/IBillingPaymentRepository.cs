using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingPaymentRepository {
    Task<BillingPayment?> GetByExternalPaymentIdAsync(
        string provider,
        string externalPaymentId,
        CancellationToken cancellationToken = default);

    Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default);
}
