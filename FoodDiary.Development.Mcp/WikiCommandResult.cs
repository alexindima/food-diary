namespace FoodDiary.Development.Mcp;

public sealed record WikiCommandResult(
    string Command,
    string RawOutput,
    string RepositoryRoot,
    string GitHead,
    IReadOnlyList<string> OutputLines,
    IReadOnlyList<string> ReferencedPaths,
    IReadOnlyList<string> RequiredChecks,
    IReadOnlyList<string> Warnings,
    bool ReadOnly = true);
