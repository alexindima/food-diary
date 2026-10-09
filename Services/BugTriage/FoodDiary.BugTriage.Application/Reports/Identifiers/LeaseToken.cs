namespace FoodDiary.BugTriage.Application.Reports.Identifiers;

public readonly record struct LeaseToken(Guid Value) {
    public static LeaseToken New() => new(Guid.NewGuid());
    public static LeaseToken Empty => new(Guid.Empty);
    public static explicit operator LeaseToken(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
