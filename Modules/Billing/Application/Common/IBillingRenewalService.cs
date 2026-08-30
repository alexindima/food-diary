using FoodDiary.Application.Billing.Models;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingRenewalService {
    Task<BillingRenewalRunResult> RenewDueSubscriptionsAsync(
        string provider,
        int batchSize,
        CancellationToken cancellationToken = default);
}
