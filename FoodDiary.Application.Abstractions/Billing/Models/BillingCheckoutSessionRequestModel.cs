namespace FoodDiary.Application.Abstractions.Billing.Models;

public sealed record BillingCheckoutSessionRequestModel(
    Guid UserId,
    string Email,
    string Plan,
    string? ExistingCustomerId);
