namespace FoodDiary.BugTriage.Application.Reports;

public sealed record ReportSummary(Guid Id, string Status, int Attempt, string? Summary, string? MergeRequestUrl);
