namespace FoodDiary.BugTriage.Application.Reports;

public sealed record ReportCompletion(string Outcome, string Summary, string? MergeRequestUrl) {
    public bool IsValid() => ReportOutcome.IsValid(Outcome) &&
        !string.IsNullOrWhiteSpace(Summary) && Summary.Length <= 8000 &&
        (MergeRequestUrl is null || (MergeRequestUrl.Length <= 2048 &&
            Uri.TryCreate(MergeRequestUrl, UriKind.Absolute, out Uri? uri) &&
            string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) && string.IsNullOrEmpty(uri.UserInfo))) &&
        (!string.Equals(Outcome, ReportOutcome.DraftReady, StringComparison.Ordinal) || MergeRequestUrl is not null);
}
