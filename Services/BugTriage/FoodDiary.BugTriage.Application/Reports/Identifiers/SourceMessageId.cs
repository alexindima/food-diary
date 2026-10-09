namespace FoodDiary.BugTriage.Application.Reports.Identifiers;

public readonly record struct SourceMessageId(Guid Value) {
    public static SourceMessageId New() => new(Guid.NewGuid());
    public static SourceMessageId Empty => new(Guid.Empty);
    public static explicit operator SourceMessageId(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
