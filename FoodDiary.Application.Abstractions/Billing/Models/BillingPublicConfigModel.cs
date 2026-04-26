namespace FoodDiary.Application.Abstractions.Billing.Models;

public sealed record BillingPublicConfigModel(
    string Provider,
    string? PaddleClientToken);
