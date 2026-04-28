namespace FoodDiary.Presentation.Api.Features.Billing.Responses;

public sealed record BillingOverviewHttpResponse(
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
