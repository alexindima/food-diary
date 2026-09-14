namespace FoodDiary.Modules.Billing.Domain.Contracts;

public static class BillingPremiumAccessPolicy {
    public static bool GrantsPremiumAccess(string? status, DateTime? currentPeriodEndUtc, DateTime nowUtc) =>
        status?.Trim().ToLowerInvariant() switch {
            "active" => true,
            "trialing" or "past_due" => currentPeriodEndUtc > nowUtc,
            _ => false,
        };
}
