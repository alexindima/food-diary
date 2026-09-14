using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingPaymentReadRepository {
    Task<BillingPayment?> GetByExternalPaymentIdAsync(
        string provider,
        string externalPaymentId,
        CancellationToken cancellationToken = default);
}
