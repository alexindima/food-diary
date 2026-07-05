namespace FoodDiary.Integrations.Options;

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

    public static bool HasAnyConfiguration(StripeOptions options) =>
        !string.IsNullOrWhiteSpace(options.SecretKey) ||
        !string.IsNullOrWhiteSpace(options.PublishableKey) ||
        !string.IsNullOrWhiteSpace(options.WebhookSecret) ||
        !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) ||
        !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId);

    public static bool HasValidConfiguration(StripeOptions options) =>
        !string.IsNullOrWhiteSpace(options.SecretKey) &&
        !string.IsNullOrWhiteSpace(options.PublishableKey) &&
        !string.IsNullOrWhiteSpace(options.WebhookSecret) &&
        !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId) &&
        Uri.IsWellFormedUriString(options.SuccessUrl, UriKind.Absolute) &&
        Uri.IsWellFormedUriString(options.CancelUrl, UriKind.Absolute) &&
        Uri.IsWellFormedUriString(options.PortalReturnUrl, UriKind.Absolute);
}
