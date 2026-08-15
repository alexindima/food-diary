using System.Text.Json;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class WikiCommandResultTests {
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
}
