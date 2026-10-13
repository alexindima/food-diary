namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminBugReportEntry(Guid Id, Guid? SourceMessageId, DateTimeOffset ReceivedAtUtc,
    string Subject, string Status, int Attempt, string? Summary, string? MergeRequestUrl, bool ContentExpired) {
    public string Source { get; init; } = "email";
    public string? Title { get; init; }
    public string? ShortSummary { get; init; }
    public Guid? AuditRunId { get; init; }
    public DateTimeOffset? AuditFromUtc { get; init; }
    public DateTimeOffset? AuditToUtc { get; init; }
}
