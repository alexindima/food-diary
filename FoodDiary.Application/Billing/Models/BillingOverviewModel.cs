namespace FoodDiary.Application.Billing.Models;

public sealed record BillingOverviewModel(
    bool IsPremium,
    string? SubscriptionStatus,
    string? Plan,
    DateTime? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    bool ManageBillingAvailable,
    string Provider,
    string? PaddleClientToken,
    IReadOnlyList<string> AvailableProviders);
