namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminAuditRunSummary(Guid Id, DateTimeOffset FromUtc, DateTimeOffset ToUtc, DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string Status, string? Summary, int FindingCount, bool ContentExpired,
    bool IsStale, DateTimeOffset? LastSuccessfulAtUtc);
