namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingCancellationRequestModel(string ExternalSubscriptionId, string ExternalCustomerId,
    string? ExternalPaymentMethodId = null, DateTime? CurrentPeriodStartUtc = null, DateTime? CurrentPeriodEndUtc = null);
