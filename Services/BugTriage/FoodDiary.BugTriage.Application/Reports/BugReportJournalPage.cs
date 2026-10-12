using FoodDiary.BugTriage.Application.AuditRuns;

namespace FoodDiary.BugTriage.Application.Reports;

public sealed record BugReportJournalPage(IReadOnlyList<BugReportJournalEntry> Items, long TotalItems) {
    public AuditRunSummary? LatestAudit { get; init; }
}
