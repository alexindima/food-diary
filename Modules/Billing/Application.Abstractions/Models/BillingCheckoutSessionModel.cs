namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingCheckoutSessionModel(
    string SessionId,
    string Url,
    string CustomerId,
    string PriceId,
    string Plan);
