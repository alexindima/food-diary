namespace FoodDiary.BugTriage.Application.AuditRuns;

public readonly record struct AuditRunId(Guid Value) {
    public static explicit operator AuditRunId(Guid value) => new(value);
}
