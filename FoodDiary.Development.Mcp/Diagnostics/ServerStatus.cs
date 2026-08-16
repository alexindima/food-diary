namespace FoodDiary.Development.Mcp.Diagnostics;

public sealed record ServerStatus(
    string Version,
    string RepositoryRoot,
    string GitHead,
    bool WikiAvailable,
    bool IndexFilesPresent,
    string DeepFreshness,
    string LastVerifiedCommit,
    string IndexStatusCode,
    string IndexCheckSummary,
    IReadOnlyList<WikiIndexStatus> Indexes,
    DateTimeOffset CheckedAtUtc,
    bool ReadOnly = true);
