namespace FoodDiary.Modules.Billing.Presentation.Responses;

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
    DateTime? PremiumTrialStartUtc,
    DateTime? PremiumTrialEndUtc,
    bool PremiumTrialActive,
    bool PremiumTrialUsed,
    bool CanStartPremiumTrial,
    string Provider,
    string? PaddleClientToken,
    IReadOnlyList<string> AvailableProviders);
