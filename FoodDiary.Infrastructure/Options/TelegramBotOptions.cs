namespace FoodDiary.Infrastructure.Options;

public sealed class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    public string ApiSecret { get; init; } = string.Empty;
}
