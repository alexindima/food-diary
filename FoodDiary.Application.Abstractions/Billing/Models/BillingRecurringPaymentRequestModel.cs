namespace FoodDiary.Application.Abstractions.Billing.Models;

public sealed record BillingRecurringPaymentRequestModel(
    Guid UserId,
    Guid BillingSubscriptionId,
    string CustomerId,
    string PaymentMethodId,
    string Plan,
    DateTime? CurrentPeriodEndUtc,
    string IdempotenceKey);
