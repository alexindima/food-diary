namespace FoodDiary.Development.Mcp.Protocol;

public sealed record WikiContextSearchResult(
    string Authority,
    string Reader,
    bool Ready,
    int IndexedDocuments,
    string? Fingerprint,
    string? UpdatedAtUtc,
    string? ChangeSetFingerprint,
    string? GitHead,
    bool Fresh,
    IReadOnlyList<string> QueryTerms,
    IReadOnlyList<WikiContextSearchCandidate> Candidates,
    double QueryDurationMilliseconds,
    string? UnavailableReason = null) {
    public WikiContextSearchResult ToCompact() => this with {
        QueryTerms = QueryTerms.Take(24).ToArray(),
        Candidates = Candidates.Take(20).ToArray(),
    };
}
