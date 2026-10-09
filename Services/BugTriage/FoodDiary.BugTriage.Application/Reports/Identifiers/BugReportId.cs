namespace FoodDiary.BugTriage.Application.Reports.Identifiers;

public readonly record struct BugReportId(Guid Value) {
    public static BugReportId New() => new(Guid.NewGuid());
    public static BugReportId Empty => new(Guid.Empty);
    public static explicit operator BugReportId(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
