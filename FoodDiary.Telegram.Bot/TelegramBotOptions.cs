namespace FoodDiary.Telegram.Bot;

public sealed class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    public string Token { get; init; } = string.Empty;
    public string WebAppUrl { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
}
