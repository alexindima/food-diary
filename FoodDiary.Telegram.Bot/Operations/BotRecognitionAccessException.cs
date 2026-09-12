namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class BotRecognitionAccessException(string code) : Exception("Recognition access was refused.") {
    internal string Code { get; } = code;
}
