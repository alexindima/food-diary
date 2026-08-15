using System.Text.Json;

namespace FoodDiary.Development.Mcp;

public sealed record WikiCommandResult(
    string Command,
    string? RawOutput,
    JsonElement? StructuredOutput,
    string RepositoryRoot,
    string GitHead,
    IReadOnlyList<string> OutputLines,
    IReadOnlyList<string> ReferencedPaths,
    IReadOnlyList<string> RequiredChecks,
    IReadOnlyList<string> Warnings,
    bool ReadOnly = true) {
    public WikiCommandResult WithoutRawOutput() => this with {
        RawOutput = null,
        OutputLines = [],
    };
}
