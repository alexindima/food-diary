using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingRecurringProviderGateway {
    string Provider { get; }

    Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
        BillingRecurringPaymentRequestModel request,
        CancellationToken cancellationToken = default);

    Task<Result<BillingRecurringPaymentModel>> GetRecurringPaymentAsync(
        string paymentId,
        BillingRecurringPaymentRequestModel request,
        CancellationToken cancellationToken = default);
}
