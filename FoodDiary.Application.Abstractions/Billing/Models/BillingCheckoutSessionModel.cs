namespace FoodDiary.Application.Billing.Models;

public sealed record BillingCheckoutSessionModel(
    string SessionId,
    string Url,
    string CustomerId,
    string PriceId,
    string Plan);
