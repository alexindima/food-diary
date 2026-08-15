using System.Text.Json;

namespace FoodDiary.Development.Mcp.Wiki;

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

    public WikiCommandResult ToCompactChangeContext(int pathLimit = 20, bool includeRawOutput = false) {
        if (StructuredOutput is not { ValueKind: JsonValueKind.Object } output) {
            return WithoutRawOutput();
        }

        JsonElement change = GetObject(output, "change");
        JsonElement paths = GetArray(change, "paths");
        JsonElement[] selectedPaths = [.. paths.EnumerateArray().Take(pathLimit).Select(item => item.Clone())];
        Dictionary<string, object?> compactChange = new(StringComparer.Ordinal) {
            ["intent"] = GetOptional(change, "intent"),
            ["scopes"] = GetOptional(change, "scopes"),
            ["directModules"] = GetOptional(change, "directModules"),
            ["downstreamModules"] = GetOptional(change, "downstreamModules"),
            ["proposedPaths"] = GetOptional(change, "proposedPaths"),
            ["pathCount"] = paths.GetArrayLength(),
            ["paths"] = selectedPaths,
            ["pathsTruncated"] = paths.GetArrayLength() > selectedPaths.Length,
        };
        Dictionary<string, object?> summary = new(StringComparer.Ordinal) {
            ["compact"] = true,
            ["analysis"] = GetOptional(output, "analysis"),
            ["risk"] = GetOptional(output, "risk"),
            ["change"] = compactChange,
            ["instructions"] = GetOptional(output, "instructions"),
            ["contextPages"] = GetOptional(output, "contextPages"),
            ["focusedTests"] = GetOptional(output, "focusedTests"),
            ["testScenarios"] = GetOptional(output, "testScenarios"),
            ["requiredChecks"] = GetOptional(output, "requiredChecks"),
            ["reviewObligations"] = GetOptional(output, "reviewObligations"),
            ["structuralViolations"] = GetOptional(output, "structuralViolations"),
            ["impactCounts"] = GetOptional(output, "impactCounts"),
            ["warnings"] = GetOptional(output, "warnings"),
            ["nextSteps"] = GetOptional(output, "nextSteps"),
        };

        return this with {
            RawOutput = includeRawOutput ? RawOutput : null,
            StructuredOutput = JsonSerializer.SerializeToElement(summary),
            OutputLines = includeRawOutput ? OutputLines : [],
            ReferencedPaths = selectedPaths.Select(path => path.GetString()!).Where(path => path is not null).ToArray(),
            RequiredChecks = [],
            Warnings = [],
        };
    }

    private static JsonElement GetObject(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Object
            ? value
            : JsonSerializer.SerializeToElement(new Dictionary<string, object?>(StringComparer.Ordinal));

    private static JsonElement GetArray(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Array
            ? value
            : JsonSerializer.SerializeToElement(Array.Empty<object>());

    private static JsonElement? GetOptional(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out JsonElement value) ? value.Clone() : null;
}
