namespace FoodDiary.Application.Abstractions.Billing.Models;

public sealed record BillingRecurringPaymentModel(
    string PaymentId,
    string PaymentMethodId,
    string? PriceId,
    string? Plan,
    string Status,
    DateTime? CurrentPeriodStartUtc,
    DateTime? CurrentPeriodEndUtc,
    string EventId,
    decimal? Amount,
    string? Currency,
    string? ProviderMetadataJson);
