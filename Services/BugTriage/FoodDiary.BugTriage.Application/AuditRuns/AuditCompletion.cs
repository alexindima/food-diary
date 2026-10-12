namespace FoodDiary.BugTriage.Application.AuditRuns;

public sealed record AuditCompletion(string Outcome, string Summary, IReadOnlyList<AuditFinding> Findings) {
    public bool IsValid() => Outcome is "clean" or "findings" or "incomplete" or "failed" &&
        !string.IsNullOrWhiteSpace(Summary) && Summary.Length <= 8000 && Findings is { Count: <= 20 } &&
        Findings.All(finding => finding?.IsValid() == true) &&
        Findings.Select(finding => finding.Id).Distinct().Count() == Findings.Count &&
        (!string.Equals(Outcome, "clean", StringComparison.Ordinal) || Findings.Count == 0) &&
        (!string.Equals(Outcome, "findings", StringComparison.Ordinal) || Findings.Count > 0);
}
