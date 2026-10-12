using FoodDiary.BugTriage.Application.Reports;

namespace FoodDiary.BugTriage.Application.AuditRuns;

public sealed record AuditFinding(Guid Id, string Title, string Outcome, string Summary, string? MergeRequestUrl) {
    public bool IsValid() => Id != Guid.Empty && !string.IsNullOrWhiteSpace(Title) && Title.Length <= 1000 &&
        new ReportCompletion(Outcome, Summary, MergeRequestUrl).IsValid();
}
