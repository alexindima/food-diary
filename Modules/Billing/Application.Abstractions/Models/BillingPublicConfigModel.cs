namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingPublicConfigModel(
    string Provider,
    string? PaddleClientToken,
    IReadOnlyList<string> AvailableProviders);
