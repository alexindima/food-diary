namespace FoodDiary.Domain.Entities.Billing;

public static class BillingProviderNames {
    public const string Stripe = "Stripe";
    public const string Paddle = "Paddle";
    public const string YooKassa = "YooKassa";

    public static bool IsSupported(string provider) =>
        string.Equals(provider, Stripe, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, Paddle, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, YooKassa, StringComparison.OrdinalIgnoreCase);
}
