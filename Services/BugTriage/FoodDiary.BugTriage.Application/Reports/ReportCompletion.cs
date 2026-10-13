namespace FoodDiary.BugTriage.Application.Reports;

public sealed record ReportCompletion(string Outcome, string Summary, string? MergeRequestUrl) {
    public string? Title { get; init; }
    public string? ShortSummary { get; init; }
    public bool IsValid() => ReportOutcome.IsValid(Outcome) &&
        !string.IsNullOrWhiteSpace(Summary) && Summary.Length <= 8000 &&
        (Title is null || (!string.IsNullOrWhiteSpace(Title) && Title.Length <= 240)) &&
        (ShortSummary is null || (!string.IsNullOrWhiteSpace(ShortSummary) && ShortSummary.Length <= 600)) &&
        (MergeRequestUrl is null || (MergeRequestUrl.Length <= 2048 &&
            Uri.TryCreate(MergeRequestUrl, UriKind.Absolute, out Uri? uri) &&
            string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) && string.IsNullOrEmpty(uri.UserInfo))) &&
        (!string.Equals(Outcome, ReportOutcome.DraftReady, StringComparison.Ordinal) || MergeRequestUrl is not null);
}
