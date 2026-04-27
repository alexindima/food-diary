namespace FoodDiary.Application.Abstractions.Billing.Models;

public sealed record BillingRecurringPaymentRequestModel(
    Guid UserId,
    string CustomerId,
    string PaymentMethodId,
    string Plan,
    DateTime? CurrentPeriodEndUtc);
