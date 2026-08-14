namespace FoodDiary.Development.Mcp;

public sealed record ChangeSetSnapshot(
    string GitHead,
    string Fingerprint,
    IReadOnlyList<string> ChangedPaths,
    DateTimeOffset CreatedAtUtc);
