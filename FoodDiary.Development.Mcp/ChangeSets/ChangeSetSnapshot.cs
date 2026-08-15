namespace FoodDiary.Development.Mcp.ChangeSets;

public sealed record ChangeSetSnapshot(
    string GitHead,
    string Fingerprint,
    IReadOnlyList<string> ChangedPaths,
    DateTimeOffset CreatedAtUtc);
