namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminAuditRunSummaryHttpResponse(Guid Id, DateTimeOffset FromUtc, DateTimeOffset ToUtc, DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string Status, string? Summary, int FindingCount, bool ContentExpired,
    bool IsStale, DateTimeOffset? LastSuccessfulAtUtc);
