namespace FoodDiary.BugTriage.Presentation.Features.AuditRuns;

public sealed record StartAuditRunHttpRequest(DateTimeOffset FromUtc, DateTimeOffset ToUtc);
