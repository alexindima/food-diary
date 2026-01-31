namespace FoodDiary.Infrastructure.Options;

public sealed class TelegramAuthOptions
{
    public const string SectionName = "TelegramAuth";

    public string BotToken { get; init; } = string.Empty;
    public int AuthTtlSeconds { get; init; } = 86400;
}
