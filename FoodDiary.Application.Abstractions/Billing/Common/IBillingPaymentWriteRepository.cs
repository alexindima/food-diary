using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingPaymentWriteRepository : IBillingPaymentReadRepository {
    Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default);
}
