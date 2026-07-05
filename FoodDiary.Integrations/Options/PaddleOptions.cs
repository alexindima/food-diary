namespace FoodDiary.Integrations.Options;

public sealed class PaddleOptions {
    public const string SectionName = "Paddle";

    public string ApiKey { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://api.paddle.com";
    public string ClientSideToken { get; init; } = string.Empty;
    public string WebhookSecretKey { get; init; } = string.Empty;
    public string PremiumMonthlyPriceId { get; init; } = string.Empty;
    public string PremiumYearlyPriceId { get; init; } = string.Empty;
    public string CheckoutUrl { get; init; } = string.Empty;

    public static bool HasAnyConfiguration(PaddleOptions options) =>
        !string.IsNullOrWhiteSpace(options.ApiKey) ||
        !string.IsNullOrWhiteSpace(options.ClientSideToken) ||
        !string.IsNullOrWhiteSpace(options.WebhookSecretKey) ||
        !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) ||
        !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId);

    public static bool HasValidConfiguration(PaddleOptions options) =>
        !string.IsNullOrWhiteSpace(options.ApiKey) &&
        Uri.IsWellFormedUriString(options.ApiBaseUrl, UriKind.Absolute) &&
        !string.IsNullOrWhiteSpace(options.ClientSideToken) &&
        !string.IsNullOrWhiteSpace(options.WebhookSecretKey) &&
        !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId) &&
        Uri.IsWellFormedUriString(options.CheckoutUrl, UriKind.Absolute);
}
