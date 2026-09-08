namespace FoodDiary.BugTriage.Application.Reports;

public sealed record BugReportJournalFilter(int Page, int Limit, DateTimeOffset? FromUtc, DateTimeOffset? ToUtc,
    string? Status, string? Search, Guid? Id);
