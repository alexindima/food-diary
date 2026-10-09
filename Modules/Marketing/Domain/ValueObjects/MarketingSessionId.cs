namespace FoodDiary.Modules.Marketing.Domain.ValueObjects;

public readonly record struct MarketingSessionId(string Value) {
    public static MarketingSessionId Empty => new(string.Empty);
    public override string ToString() => Value;
}
