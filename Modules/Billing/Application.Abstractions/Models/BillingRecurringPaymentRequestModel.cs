namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingRecurringPaymentRequestModel(
    Guid UserId,
    FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids.BillingSubscriptionId BillingSubscriptionId,
    string CustomerId,
    string PaymentMethodId,
    string Plan,
    DateTime? CurrentPeriodEndUtc,
    string IdempotenceKey);
