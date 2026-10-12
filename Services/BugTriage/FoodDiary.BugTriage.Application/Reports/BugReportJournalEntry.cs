namespace FoodDiary.BugTriage.Application.Reports;

public sealed record BugReportJournalEntry(Guid Id, Guid? SourceMessageId, DateTimeOffset ReceivedAtUtc,
    string Subject, string Status, int Attempt, string? Summary, string? MergeRequestUrl, bool ContentExpired) {
    public string Source { get; init; } = "email";
    public Guid? AuditRunId { get; init; }
    public DateTimeOffset? AuditFromUtc { get; init; }
    public DateTimeOffset? AuditToUtc { get; init; }
}
