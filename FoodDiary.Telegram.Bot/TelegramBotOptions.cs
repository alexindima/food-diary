namespace FoodDiary.Telegram.Bot;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class TelegramBotOptions {
    public const string SectionName = "TelegramBot";

    public string Token { get; init; } = string.Empty;
    public string WebAppUrl { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public bool OperationsEnabled { get; init; }

    public static bool HasOperationConfiguration(TelegramBotOptions options) {
        return !options.OperationsEnabled ||
               (!string.IsNullOrWhiteSpace(options.Token) &&
                !string.IsNullOrWhiteSpace(options.ApiSecret) && options.ApiSecret.Length >= 16 &&
                BotUriHelper.TryCreateApiBaseUri(options.ApiBaseUrl, out _) &&
                Uri.TryCreate(options.WebAppUrl, UriKind.Absolute, out Uri? webApp) &&
                string.Equals(webApp.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) &&
                webApp.UserInfo.Length == 0 && webApp.Query.Length == 0 && webApp.Fragment.Length == 0);
    }

    public static bool HasValidWebAppUrl(TelegramBotOptions options) {
        return string.IsNullOrWhiteSpace(options.WebAppUrl) ||
               Uri.IsWellFormedUriString(options.WebAppUrl, UriKind.Absolute);
    }

    public static bool HasValidApiBaseUrl(TelegramBotOptions options) {
        return string.IsNullOrWhiteSpace(options.ApiBaseUrl) ||
               BotUriHelper.TryCreateApiBaseUri(options.ApiBaseUrl, out _);
    }

    public static bool HasValidApiSecret(TelegramBotOptions options) {
        return string.IsNullOrWhiteSpace(options.ApiSecret) || options.ApiSecret.Length >= 16;
    }
}
