namespace FoodDiary.BugTriage.Application.Reports;

public static class ReportOutcome {
    public const string NeedsInformation = "needs_information";
    public const string NotConfirmed = "not_confirmed";
    public const string Duplicate = "duplicate";
    public const string DraftReady = "draft_ready";
    public const string Failed = "failed";

    public static bool IsValid(string value) => value is NeedsInformation or NotConfirmed or Duplicate or DraftReady or Failed;
}
