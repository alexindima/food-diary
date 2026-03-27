namespace FoodDiary.Telegram.Bot;

public sealed class TelegramBotOptions {
    public const string SectionName = "TelegramBot";

    public string Token { get; init; } = string.Empty;
    public string WebAppUrl { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;

    public static bool HasValidWebAppUrl(TelegramBotOptions options) {
        return string.IsNullOrWhiteSpace(options.WebAppUrl) ||
               Uri.IsWellFormedUriString(options.WebAppUrl, UriKind.Absolute);
    }

    public static bool HasValidApiBaseUrl(TelegramBotOptions options) {
        return string.IsNullOrWhiteSpace(options.ApiBaseUrl) ||
               Uri.IsWellFormedUriString(options.ApiBaseUrl, UriKind.Absolute);
    }

    public static bool HasValidApiSecret(TelegramBotOptions options) {
        return string.IsNullOrWhiteSpace(options.ApiSecret) || options.ApiSecret.Length >= 16;
    }
}
