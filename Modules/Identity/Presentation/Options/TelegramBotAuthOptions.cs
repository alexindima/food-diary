namespace FoodDiary.Presentation.Api.Options;

public sealed class TelegramBotAuthOptions {
    public const string SectionName = "TelegramBot";

    public string ApiSecret { get; init; } = string.Empty;

    public static bool HasValidApiSecret(TelegramBotAuthOptions options) {
        return string.IsNullOrWhiteSpace(options.ApiSecret) || options.ApiSecret.Length >= 16;
    }
}
