using System.Text.Json;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProtocolCompactionTests {
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
