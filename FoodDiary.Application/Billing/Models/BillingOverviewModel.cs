namespace FoodDiary.Application.Billing.Models;

public sealed record BillingOverviewModel(
    bool IsPremium,
    string? SubscriptionStatus,
    string? Plan,
    string? SubscriptionProvider,
    DateTime? CurrentPeriodStartUtc,
    DateTime? CurrentPeriodEndUtc,
    DateTime? NextBillingAttemptUtc,
    bool CancelAtPeriodEnd,
    bool RenewalEnabled,
    bool ManageBillingAvailable,
    string Provider,
    string? PaddleClientToken,
    IReadOnlyList<string> AvailableProviders);
