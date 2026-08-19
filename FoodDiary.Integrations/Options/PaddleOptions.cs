namespace FoodDiary.Integrations.Options;

public sealed class PaddleOptions {
    public const string SectionName = "Paddle";
    public const string SandboxEnvironment = "Sandbox";
    public const string ProductionEnvironment = "Production";

    public string Environment { get; init; } = SandboxEnvironment;
    public bool CheckoutEnabled { get; init; } = true;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://api.paddle.com";
    public string ClientSideToken { get; init; } = string.Empty;
    public string WebhookSecretKey { get; init; } = string.Empty;
    public string NotificationSettingId { get; init; } = string.Empty;
    public string PremiumMonthlyPriceId { get; init; } = string.Empty;
    public string PremiumYearlyPriceId { get; init; } = string.Empty;
    public string CheckoutUrl { get; init; } = string.Empty;
    public int WebhookTimestampToleranceSeconds { get; init; } = 300;

    public static bool HasAnyConfiguration(PaddleOptions options) =>
        !string.IsNullOrWhiteSpace(options.ApiKey) ||
        !string.IsNullOrWhiteSpace(options.ClientSideToken) ||
        !string.IsNullOrWhiteSpace(options.WebhookSecretKey) ||
        !string.IsNullOrWhiteSpace(options.NotificationSettingId) ||
        !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) ||
        !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId);

    public static bool HasValidConfiguration(PaddleOptions options) =>
        !string.IsNullOrWhiteSpace(options.ApiKey) &&
        Uri.IsWellFormedUriString(options.ApiBaseUrl, UriKind.Absolute) &&
        !string.IsNullOrWhiteSpace(options.ClientSideToken) &&
        !string.IsNullOrWhiteSpace(options.WebhookSecretKey) &&
        HasValidNotificationRecoveryConfiguration(options) &&
        !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId) &&
        Billing.BillingUrlValidator.IsAbsoluteHttps(options.CheckoutUrl) &&
        options.WebhookTimestampToleranceSeconds is >= 5 and <= 900 &&
        HasMatchingEnvironment(options);

    public static bool HasConfiguredNotificationRecovery(PaddleOptions options) =>
        IsValidNotificationSettingId(options.NotificationSettingId);

    private static bool IsValidNotificationSettingId(string value) =>
        value.Trim().StartsWith("ntfset_", StringComparison.Ordinal) && value.Trim().Length == 33;

    private static bool HasValidNotificationRecoveryConfiguration(PaddleOptions options) =>
        !string.Equals(options.Environment, ProductionEnvironment, StringComparison.OrdinalIgnoreCase) ||
        HasConfiguredNotificationRecovery(options);

    public static bool HasMatchingEnvironment(PaddleOptions options) {
        string clientSideToken = options.ClientSideToken.Trim();
        if (string.Equals(options.Environment, SandboxEnvironment, StringComparison.OrdinalIgnoreCase)) {
            return string.Equals(options.ApiBaseUrl.TrimEnd('/'), "https://sandbox-api.paddle.com", StringComparison.OrdinalIgnoreCase) &&
                clientSideToken.StartsWith("test_", StringComparison.Ordinal);
        }

        if (string.Equals(options.Environment, ProductionEnvironment, StringComparison.OrdinalIgnoreCase)) {
            return string.Equals(options.ApiBaseUrl.TrimEnd('/'), "https://api.paddle.com", StringComparison.OrdinalIgnoreCase) &&
                !clientSideToken.StartsWith("test_", StringComparison.Ordinal) &&
                !options.ApiKey.Contains("sdbx", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
