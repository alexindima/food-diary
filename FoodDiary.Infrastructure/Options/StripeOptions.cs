namespace FoodDiary.Infrastructure.Options;

public sealed class StripeOptions {
    public const string SectionName = "Stripe";

    public string SecretKey { get; init; } = string.Empty;
    public string PublishableKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public string PremiumMonthlyPriceId { get; init; } = string.Empty;
    public string PremiumYearlyPriceId { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
    public string PortalReturnUrl { get; init; } = string.Empty;
}
