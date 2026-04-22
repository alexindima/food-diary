namespace FoodDiary.Application.Billing.Models;

public sealed record BillingPublicConfigModel(
    string Provider,
    string? PaddleClientToken);
