using System.Text.Json;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class WikiCommandResultTests {
    [Fact]
    public void ToCompactTrace_LimitsCollectionsAndCanPreserveRawOutput() {
        JsonElement structuredOutput = JsonSerializer.SerializeToElement(new {
            query = "user flow",
            symbols = new[] { new { path = "one.cs" }, new { path = "two.cs" } },
            consumers = new[] { new { path = "consumer.cs" } },
            candidates = new[] { new { path = "candidate.cs" } },
            ranking = new { strategy = "hybrid" },
        });
        WikiCommandResult result = new(
            "trace", "raw", structuredOutput, "repository", "head", ["line"],
            ["one.cs", "two.cs", "consumer.cs"], [], ["warning-one", "warning-two"],
            ["one.cs", "two.cs", "consumer.cs"]);

        WikiCommandResult compact = result.ToCompactTrace(itemLimit: 1, includeRawOutput: true);

        JsonElement summary = Assert.IsType<JsonElement>(compact.StructuredOutput);
        Assert.Multiple(
            () => Assert.Equal("raw", compact.RawOutput),
            () => Assert.Single(compact.OutputLines),
            () => Assert.Single(compact.ReferencedPaths),
            () => Assert.Single(compact.ScopePaths!),
            () => Assert.True(summary.GetProperty("scopePathsTruncated").GetBoolean()),
            () => Assert.Equal(2, summary.GetProperty("symbolCount").GetInt32()));
    }

    [Fact]
    public void ToCompactTrace_WithNonObjectOutput_DropsRawOutput() {
        WikiCommandResult result = CreateResult(JsonSerializer.SerializeToElement(new[] { "not-object" }));

        WikiCommandResult compact = result.ToCompactTrace();

        Assert.Null(compact.RawOutput);
        Assert.Empty(compact.OutputLines);
    }

    [Fact]
    public void ToCompactTestPlan_UsesFallbackFocusedTestsAndTruncatesCollections() {
        JsonElement structuredOutput = JsonSerializer.SerializeToElement(new {
            baseline = "head",
            focusedTests = new[] { "one", "two" },
            commands = new[] { "command-one", "command-two" },
            scenarios = new[] { "scenario-one", "scenario-two" },
            reviewObligations = new[] { "review" },
            warnings = new[] { "warning" },
        });
        WikiCommandResult result = CreateResult(structuredOutput);

        WikiCommandResult compact = result.ToCompactTestPlan(itemLimit: 1);

        JsonElement summary = Assert.IsType<JsonElement>(compact.StructuredOutput);
        Assert.Multiple(
            () => Assert.True(summary.GetProperty("truncated").GetBoolean()),
            () => Assert.Single(summary.GetProperty("focusedTests").EnumerateArray()),
            () => Assert.Single(summary.GetProperty("commands").EnumerateArray()),
            () => Assert.Single(compact.RequiredChecks),
            () => Assert.Single(compact.Warnings));
    }

    [Fact]
    public void ToCompactTestPlan_WithNonObjectOutput_DropsRawOutput() {
        WikiCommandResult result = CreateResult(JsonSerializer.SerializeToElement("not-object"));

        WikiCommandResult compact = result.ToCompactTestPlan();

        Assert.Null(compact.RawOutput);
    }

    [Fact]
    public void ToCompactChangeContext_LimitsPathsAndKeepsVerificationGuidance() {
        string[] paths = [.. Enumerable.Range(1, 25).Select(index => $"FoodDiary.Web.Client/src/app/path-{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}.ts")];
        JsonElement structuredOutput = JsonSerializer.SerializeToElement(new {
            analysis = new { mode = "working-tree" },
            risk = new { level = "high", score = 10 },
            change = new {
                intent = "Change measurements",
                paths,
                scopes = new[] { "Frontend" },
                directModules = new[] { "Dashboard" },
                downstreamModules = Array.Empty<string>(),
                proposedPaths = new[] { "FoodDiary.Web.Client/src/app" },
            },
            focusedTests = new[] { "FoodDiary.Web.Client/src/app/path-1.spec.ts" },
            requiredChecks = new[] { new { id = "frontend-verify", command = "npm run verify" } },
            reviewObligations = new[] { new { id = "frontend-visual-evidence" } },
            rolloutPlan = new { largeDuplicatedPayload = new string('x', 10_000) },
        });
        WikiCommandResult result = new(
            "brief",
            "large raw output",
            structuredOutput,
            "repository",
            "head",
            ["line"],
            paths,
            ["npm run verify"],
            ["warning"]);

        WikiCommandResult compact = result.ToCompactChangeContext();

        Assert.Null(compact.RawOutput);
        Assert.Empty(compact.OutputLines);
        Assert.Equal(20, compact.ReferencedPaths.Count);
        JsonElement summary = compact.StructuredOutput!.Value;
        Assert.True(summary.GetProperty("compact").GetBoolean());
        Assert.Equal(25, summary.GetProperty("change").GetProperty("pathCount").GetInt32());
        Assert.True(summary.GetProperty("change").GetProperty("pathsTruncated").GetBoolean());
        Assert.Equal("frontend-verify", summary.GetProperty("requiredChecks")[0].GetProperty("id").GetString());
        Assert.False(summary.TryGetProperty("rolloutPlan", out _));
    }

    [Fact]
    public void ToCompactChangeContext_PreservesRawDiagnosticsWhenRequested() {
        JsonElement structuredOutput = JsonSerializer.SerializeToElement(new {
            change = new { paths = Array.Empty<string>() },
        });
        WikiCommandResult result = new(
            "brief",
            "raw diagnostics",
            structuredOutput,
            "repository",
            "head",
            ["raw diagnostics"],
            [],
            [],
            []);

        WikiCommandResult compact = result.ToCompactChangeContext(includeRawOutput: true);

        Assert.Equal("raw diagnostics", compact.RawOutput);
        Assert.Single(compact.OutputLines);
    }

    private static WikiCommandResult CreateResult(JsonElement structuredOutput) =>
        new(
            "command",
            "raw",
            structuredOutput,
            "repository",
            "head",
            ["line"],
            ["one", "two"],
            ["check-one", "check-two"],
            ["warning-one", "warning-two"]);
}
