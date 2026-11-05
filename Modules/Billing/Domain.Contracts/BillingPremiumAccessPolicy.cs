namespace FoodDiary.Modules.Billing.Domain.Contracts;

public static class BillingPremiumAccessPolicy {
    public static bool GrantsPremiumAccess(string? status, DateTime? currentPeriodEndUtc, DateTime nowUtc, string? provider = null) =>
        status?.Trim().ToLowerInvariant() switch {
            "active" when string.Equals(provider, BillingProviderNames.TelegramStars, StringComparison.OrdinalIgnoreCase) => currentPeriodEndUtc > nowUtc,
            "active" => true,
            "trialing" or "past_due" => currentPeriodEndUtc > nowUtc,
            _ => false,
        };
}
