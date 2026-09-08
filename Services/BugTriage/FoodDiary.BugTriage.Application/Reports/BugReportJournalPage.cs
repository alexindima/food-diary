namespace FoodDiary.BugTriage.Application.Reports;

public sealed record BugReportJournalPage(IReadOnlyList<BugReportJournalEntry> Items, long TotalItems);
