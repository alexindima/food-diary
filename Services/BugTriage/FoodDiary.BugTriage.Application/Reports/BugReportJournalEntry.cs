namespace FoodDiary.BugTriage.Application.Reports;

public sealed record BugReportJournalEntry(Guid Id, Guid SourceMessageId, DateTimeOffset ReceivedAtUtc,
    string Subject, string Status, int Attempt, string? Summary, string? MergeRequestUrl, bool ContentExpired);
