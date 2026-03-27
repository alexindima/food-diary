namespace FoodDiary.Telegram.Bot;

public sealed class TelegramBotOptions {
    public const string SectionName = "TelegramBot";

    public string Token { get; init; } = string.Empty;
    public string WebAppUrl { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;

    public static bool HasValidWebAppUrl(string? value) {
        return string.IsNullOrWhiteSpace(value) || Uri.IsWellFormedUriString(value, UriKind.Absolute);
    }

    public static bool HasValidApiBaseUrl(string? value) {
        return string.IsNullOrWhiteSpace(value) || Uri.IsWellFormedUriString(value, UriKind.Absolute);
    }

    public static bool HasValidApiSecret(string? value) {
        return string.IsNullOrWhiteSpace(value) || value.Length >= 16;
    }
}
