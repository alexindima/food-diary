namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingCancellationRequestModel(string ExternalSubscriptionId, string ExternalCustomerId);
