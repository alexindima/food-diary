using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingRecurringProviderGateway {
    string Provider { get; }

    Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
        BillingRecurringPaymentRequestModel request,
        CancellationToken cancellationToken = default);
}
