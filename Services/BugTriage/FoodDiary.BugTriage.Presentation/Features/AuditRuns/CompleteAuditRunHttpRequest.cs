using FoodDiary.BugTriage.Application.AuditRuns;

namespace FoodDiary.BugTriage.Presentation.Features.AuditRuns;

public sealed record CompleteAuditRunHttpRequest(string Outcome, string Summary, IReadOnlyList<AuditFinding> Findings);
