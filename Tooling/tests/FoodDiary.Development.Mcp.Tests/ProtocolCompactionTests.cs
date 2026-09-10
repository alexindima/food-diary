using System.Text.Json;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProtocolCompactionTests {
    [Fact]
    public void TraceCompaction_PreservesSemanticHandlerAndHttpEvidence() {
        WikiCommandResult trace = CreateResult("trace", new {
            request = "StartFastingCommand",
            traceDepth = "handler-dependencies-plus-one-interface-hop",
            limitations = new[] { "Source navigation only" },
            unresolvedDependencies = new[] { "TimeProvider" },
            handler = new { path = "StartFastingCommandHandler.cs", line = 12 },
            presentation = new[] { new { path = "FastingController.cs", confidence = "mapping-type" } },
            tests = new[] { new { path = "FastingFeatureTests.Start.cs" } },
        }).ToCompactTrace();
        JsonElement output = trace.StructuredOutput!.Value;
        Assert.Multiple(
            () => Assert.Equal("StartFastingCommand", output.GetProperty("request").GetString()),
            () => Assert.Equal("TimeProvider", output.GetProperty("unresolvedDependencies")[0].GetString()),
            () => Assert.Single(output.GetProperty("limitations").EnumerateArray()),
            () => Assert.Equal("handler-dependencies-plus-one-interface-hop", output.GetProperty("traceDepth").GetString()),
            () => Assert.Equal(12, output.GetProperty("handler").GetProperty("line").GetInt32()),
            () => Assert.Single(output.GetProperty("presentation").EnumerateArray()),
            () => Assert.Single(output.GetProperty("tests").EnumerateArray()));
    }

    [Fact]
    public void TraceCompaction_BoundsNestedDependenciesAndRetainsParentEvidence() {
        WikiCommandResult trace = CreateResult("trace", new {
            nestedDependencies = new[] {
                new { parentPath = "Search.cs", contract = "ICache", status = "source-candidate" },
                new { parentPath = "Search.cs", contract = "IProvider", status = "implementation-not-resolved" },
            },
            nestedDependenciesTruncated = false,
        }).ToCompactTrace(itemLimit: 1);
        JsonElement output = trace.StructuredOutput!.Value;
        Assert.Multiple(
            () => Assert.Single(output.GetProperty("nestedDependencies").EnumerateArray()),
            () => Assert.Equal("Search.cs", output.GetProperty("nestedDependencies")[0].GetProperty("parentPath").GetString()),
            () => Assert.True(output.GetProperty("nestedDependenciesTruncated").GetBoolean()),
            () => Assert.True(output.GetProperty("truncated").GetBoolean()));
    }

    [Fact]
    public void TestPlanCompaction_PreservesGraphEvidenceAndReportsTruncation() {
        WikiCommandResult plan = CreateResult("test-plan", new {
            mode = "sqlite-graph-only",
            confidence = "low",
            required = new[] { "BuildWorkflowGuardrailTests.cs", "OtherTests.cs" },
            recommended = new[] { "ConsumerTests.cs" },
            fullRegression = new[] { "Use ordinary test-plan" },
        }).ToCompactTestPlan(itemLimit: 1);
        JsonElement output = plan.StructuredOutput!.Value;
        Assert.Multiple(
            () => Assert.Equal("BuildWorkflowGuardrailTests.cs", output.GetProperty("required")[0].GetString()),
            () => Assert.Equal("ConsumerTests.cs", output.GetProperty("recommended")[0].GetString()),
            () => Assert.Equal("low", output.GetProperty("confidence").GetString()),
            () => Assert.True(output.GetProperty("truncated").GetBoolean()));
    }

    [Fact]
    public void DevelopmentContext_CompactionAndRawRemoval_TransformNestedResults() {
        WikiCommandResult change = CreateResult("brief", new { change = new { paths = new[] { "one.cs" } } });
        WikiCommandResult trace = CreateResult("trace", new { symbols = new[] { new { path = "one.cs" } } });
        WikiCommandResult plan = CreateResult("test-plan", new { commands = new[] { "dotnet test focused" } });
        WikiContextSearchResult search = new(
            Authority: "wiki",
            Reader: "sqlite",
            Ready: true,
            IndexedDocuments: 25,
            Fingerprint: "fingerprint",
            UpdatedAtUtc: "now",
            ChangeSetFingerprint: "change",
            GitHead: "head",
            Fresh: true,
            QueryTerms: [.. Enumerable.Range(1, 30).Select(index => $"term-{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}")],
            Candidates: [.. Enumerable.Range(1, 25).Select(index => new WikiContextSearchCandidate(
                Rank: index,
                Path: $"path-{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}.cs",
                RecordType: "code",
                Category: "csharp",
                Score: 100 - index,
                LexicalRank: 1d / index,
                Reasons: []))],
            QueryDurationMilliseconds: 2.5);
        var context = new DevelopmentContext(
            SnapshotFingerprint: "snapshot",
            GitHead: "head",
            ChangeContext: change,
            BackendTrace: trace,
            TestPlan: plan,
            PartialSuccess: false,
            ComponentErrors: [],
            ExpandedScopePaths: [.. Enumerable.Range(1, 25).Select(index => $"scope-{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}")],
            ScopeMismatch: false,
            EffectiveLayers: ["Application"],
            CrossLayerScope: false,
            SqlContextSearch: search);

        DevelopmentContext compact = context.ToCompact(includeRawOutput: true);
        DevelopmentContext withoutRaw = context.WithoutRawOutput();
        WikiContextSearchResult compactSearch = Assert.IsType<WikiContextSearchResult>(compact.SqlContextSearch);

        Assert.Multiple(
            () => Assert.Equal(20, compact.ExpandedScopePaths.Count),
            () => Assert.Equal(context.ExpandedScopePaths.Take(3), compact.SuggestedStartingPaths, StringComparer.Ordinal),
            () => Assert.Equal(compact.ExpandedScopePaths, compact.SuggestedStartingPaths.Concat(compact.AdditionalCandidatePaths), StringComparer.Ordinal),
            () => Assert.Equal(17, compact.AdditionalCandidatePaths.Count),
            () => Assert.Contains("not a confirmed edit scope", compact.ScopeInterpretation, StringComparison.Ordinal),
            () => Assert.Equal(24, compactSearch.QueryTerms.Count),
            () => Assert.Equal(20, compactSearch.Candidates.Count),
            () => Assert.Equal("raw", compact.ChangeContext!.RawOutput),
            () => Assert.Null(withoutRaw.ChangeContext!.RawOutput),
            () => Assert.Null(withoutRaw.BackendTrace!.RawOutput),
            () => Assert.Null(withoutRaw.TestPlan!.RawOutput));
    }

    private static WikiCommandResult CreateResult(string command, object structured) =>
        new(
            command,
            "raw",
            JsonSerializer.SerializeToElement(structured),
            "repository",
            "head",
            ["line"],
            ["one.cs"],
            ["dotnet test focused"],
            []);
}
