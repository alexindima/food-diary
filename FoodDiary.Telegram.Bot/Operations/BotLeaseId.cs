namespace FoodDiary.Telegram.Bot.Operations;

internal readonly record struct BotLeaseId(Guid Value) {
    internal static BotLeaseId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
