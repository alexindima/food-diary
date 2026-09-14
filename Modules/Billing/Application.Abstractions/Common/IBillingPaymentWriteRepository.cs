using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingPaymentWriteRepository {
    Task<BillingPayment?> GetByExternalPaymentIdAsync(
        string provider,
        string externalPaymentId,
        CancellationToken cancellationToken = default);

    Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default);

    Task UpdateAsync(BillingPayment payment, CancellationToken cancellationToken = default);
}
