namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingCheckoutSessionRequestModel(
    Guid UserId,
    string Email,
    string Plan,
    string? ExistingCustomerId,
    string? IdempotencyKey = null);
