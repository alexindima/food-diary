namespace FoodDiary.Telegram.Bot.Operations;

internal readonly record struct BotUserId(Guid Value) {
    internal static BotUserId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
