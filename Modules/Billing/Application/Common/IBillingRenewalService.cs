using FoodDiary.Modules.Billing.Application.Models;

namespace FoodDiary.Modules.Billing.Application.Common;

public interface IBillingRenewalService {
    Task<BillingRenewalRunResult> RenewDueSubscriptionsAsync(
        string provider,
        int batchSize,
        CancellationToken cancellationToken = default);
}
