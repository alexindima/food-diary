namespace FoodDiary.BugTriage.Application.AuditRuns;

public sealed record AuditRunSummary(Guid Id, DateTimeOffset FromUtc, DateTimeOffset ToUtc, DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string Status, string? Summary, int FindingCount, bool ContentExpired,
    bool IsStale, DateTimeOffset? LastSuccessfulAtUtc);
