namespace FoodDiary.Telegram.Bot.Operations;

internal readonly record struct BotOperationId(Guid Value) {
    internal static BotOperationId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
