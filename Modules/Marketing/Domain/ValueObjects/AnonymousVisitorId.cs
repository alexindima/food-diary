namespace FoodDiary.Modules.Marketing.Domain.ValueObjects;

public readonly record struct AnonymousVisitorId(string Value) {
    public static AnonymousVisitorId Empty => new(string.Empty);
    public override string ToString() => Value;
}
