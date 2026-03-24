namespace FoodDiary.Presentation.Api.Options;

public sealed class TelegramBotAuthOptions {
    public const string SectionName = "TelegramBot";

    public string ApiSecret { get; init; } = string.Empty;
}
