namespace FoodDiary.Application.Billing.Models;

public sealed record BillingCheckoutSessionRequestModel(
    Guid UserId,
    string Email,
    string Plan,
    string? ExistingCustomerId);
