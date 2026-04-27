namespace FoodDiary.Presentation.Api.Features.Billing.Responses;

public sealed record BillingOverviewHttpResponse(
    bool IsPremium,
    string? SubscriptionStatus,
    string? Plan,
    DateTime? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    bool ManageBillingAvailable,
    string Provider,
    string? PaddleClientToken,
    IReadOnlyList<string> AvailableProviders);
