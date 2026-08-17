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
    IReadOnlyList<string>? ScopePaths = null,
    bool ReadOnly = true) {
    public WikiCommandResult WithoutRawOutput() => this with {
        RawOutput = null,
        OutputLines = [],
    };

    public IReadOnlyList<string> GetScopePaths() => ScopePaths ?? ReferencedPaths;

    public WikiCommandResult ToCompactTrace(int itemLimit = 12, bool includeRawOutput = false) {
        if (StructuredOutput is not { ValueKind: JsonValueKind.Object } output) {
            return WithoutRawOutput();
        }
        JsonElement symbols = GetArray(output, "symbols");
        JsonElement consumers = GetArray(output, "consumers");
        JsonElement candidates = GetArray(output, "candidates");
        Dictionary<string, object?> summary = new(StringComparer.Ordinal) {
            ["compact"] = true,
            ["query"] = GetOptional(output, "query"),
            ["symbolCount"] = symbols.GetArrayLength(),
            ["symbols"] = symbols.EnumerateArray().Take(itemLimit).Select(item => item.Clone()).ToArray(),
            ["consumerCount"] = consumers.GetArrayLength(),
            ["consumers"] = consumers.EnumerateArray().Take(itemLimit).Select(item => item.Clone()).ToArray(),
            ["candidateCount"] = candidates.GetArrayLength(),
            ["candidates"] = candidates.EnumerateArray().Take(itemLimit).Select(item => item.Clone()).ToArray(),
            ["scopePaths"] = GetScopePaths().Take(itemLimit).ToArray(),
            ["scopePathsTruncated"] = GetScopePaths().Count > itemLimit,
            ["ranking"] = GetOptional(output, "ranking"),
        };
        return this with {
            RawOutput = includeRawOutput ? RawOutput : null,
            StructuredOutput = JsonSerializer.SerializeToElement(summary),
            OutputLines = includeRawOutput ? OutputLines : [],
            ReferencedPaths = ReferencedPaths.Take(itemLimit).ToArray(),
            RequiredChecks = [],
            Warnings = Warnings.Take(itemLimit).ToArray(),
            ScopePaths = GetScopePaths().Take(itemLimit).ToArray(),
        };
    }

    public WikiCommandResult ToCompactTestPlan(int itemLimit = 12, bool includeRawOutput = false) {
        if (StructuredOutput is not { ValueKind: JsonValueKind.Object } output) {
            return WithoutRawOutput();
        }
        JsonElement focusedTests = GetArray(output, "focusedTestDetails");
        if (focusedTests.GetArrayLength() == 0) {
            focusedTests = GetArray(output, "focusedTests");
        }
        JsonElement commands = GetArray(output, "commands");
        JsonElement scenarios = GetArray(output, "scenarios");
        Dictionary<string, object?> summary = new(StringComparer.Ordinal) {
            ["compact"] = true,
            ["baseline"] = GetOptional(output, "baseline"),
            ["scopes"] = GetOptional(output, "scopes"),
            ["modules"] = GetOptional(output, "modules"),
            ["focusedTestCount"] = focusedTests.GetArrayLength(),
            ["focusedTests"] = focusedTests.EnumerateArray().Take(itemLimit).Select(item => item.Clone()).ToArray(),
            ["commandCount"] = commands.GetArrayLength(),
            ["commands"] = commands.EnumerateArray().Take(itemLimit).Select(item => item.Clone()).ToArray(),
            ["scenarioCount"] = scenarios.GetArrayLength(),
            ["scenarios"] = scenarios.EnumerateArray().Take(itemLimit).Select(item => item.Clone()).ToArray(),
            ["reviewObligationIds"] = GetOptional(output, "reviewObligationIds") ?? GetOptional(output, "reviewObligations"),
            ["warnings"] = GetOptional(output, "warnings"),
            ["truncated"] = focusedTests.GetArrayLength() > itemLimit || commands.GetArrayLength() > itemLimit || scenarios.GetArrayLength() > itemLimit,
        };
        return this with {
            RawOutput = includeRawOutput ? RawOutput : null,
            StructuredOutput = JsonSerializer.SerializeToElement(summary),
            OutputLines = includeRawOutput ? OutputLines : [],
            ReferencedPaths = ReferencedPaths.Take(itemLimit).ToArray(),
            RequiredChecks = RequiredChecks.Take(itemLimit).ToArray(),
            Warnings = Warnings.Take(itemLimit).ToArray(),
        };
    }

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
