namespace FoodDiary.BugTriage.Application.AuditRuns;

public sealed record AuditWindow(DateTimeOffset FromUtc, DateTimeOffset ToUtc) {
    public bool IsValid(DateTimeOffset now) => FromUtc < ToUtc && ToUtc - FromUtc <= TimeSpan.FromDays(7) &&
        ToUtc <= now.AddMinutes(5) && FromUtc >= now.AddDays(-90);
}
