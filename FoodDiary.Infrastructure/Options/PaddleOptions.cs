namespace FoodDiary.Infrastructure.Options;

public sealed class PaddleOptions {
    public const string SectionName = "Paddle";

    public string ApiKey { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://api.paddle.com";
    public string ClientSideToken { get; init; } = string.Empty;
    public string WebhookSecretKey { get; init; } = string.Empty;
    public string PremiumMonthlyPriceId { get; init; } = string.Empty;
    public string PremiumYearlyPriceId { get; init; } = string.Empty;
    public string CheckoutUrl { get; init; } = string.Empty;
}
