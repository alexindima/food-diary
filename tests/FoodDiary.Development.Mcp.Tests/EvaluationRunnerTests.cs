using System.Reflection;
using System.Text.Json;
using NSubstitute;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class EvaluationRunnerTests {
    [Theory]
    [InlineData("null", false)]
    [InlineData("[]", false)]
    [InlineData("{}", false)]
    [InlineData("{\"commands\":[]}", false)]
    [InlineData("{\"focusedTests\":[\"focused\"]}", true)]
    public void DevelopmentContextEvaluationRunner_HasFocusedChecks_RecognizesStructuredChecks(
        string structuredJson,
        bool expected) {
        WikiCommandResult? testPlan = string.Equals(structuredJson, "null", StringComparison.Ordinal)
            ? null
            : new WikiCommandResult(
                "test-plan",
                "raw",
                JsonSerializer.Deserialize<JsonElement>(structuredJson),
                "repository",
                "head",
                [],
                [],
                [],
                []);
        MethodInfo method = typeof(DevelopmentContextEvaluationRunner).GetMethod(
            "HasFocusedChecks",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        bool actual = (bool)method.Invoke(null, [testPlan])!;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task WikiContextSearchEvaluationRunner_WithHitsAndMisses_WritesMetrics() {
        string corpusPath = await WriteCorpusAsync("""
            {
              "schemaVersion": 1,
              "diagnosticLimit": 20,
              "thresholds": { "minimumCaseCount": 2, "minimumTop1Rate": 0.5, "minimumTop10Rate": 1, "minimumMeanReciprocalRank": 0.6 },
              "switchCriteria": { "minimumCaseCount": 3, "minimumTop1Rate": 1, "minimumTop10Rate": 1, "minimumMeanReciprocalRank": 1 },
              "cases": [
                { "id": "hit", "query": "users", "changeType": "Backend", "expectedPaths": ["FoodDiary.Application.Users/Handler.cs"] },
                { "id": "second", "query": "billing", "expectedPaths": ["FoodDiary.Application.Billing/Handler.cs"], "acceptedPaths": ["Other.cs"] }
              ]
            }
            """);
        var search = new SequencedContextSearch([
            CreateSearchResult([
                new WikiContextSearchCandidate(1, "FoodDiary.Application.Users/Handler.cs", "symbol", "Application", 100, 1, []),
            ]),
            CreateSearchResult([
                new WikiContextSearchCandidate(1, "Other.cs", "symbol", "Other", 90, 1, []),
                new WikiContextSearchCandidate(2, "FoodDiary.Application.Billing/Handler.cs", "symbol", "Application", 80, 0.5, []),
            ]),
        ]);
        await using var output = new StringWriter();

        try {
            await WikiContextSearchEvaluationRunner.RunAsync(
                search,
                corpusPath,
                output,
                CancellationToken.None,
                expectedChangeSetFingerprint: "fingerprint");
        } finally {
            File.Delete(corpusPath);
        }

        using var document = JsonDocument.Parse(output.ToString());
        JsonElement root = document.RootElement;
        Assert.Multiple(
            () => Assert.True(root.GetProperty("passed").GetBoolean()),
            () => Assert.False(root.GetProperty("switchReady").GetBoolean()),
            () => Assert.Equal(2, root.GetProperty("caseCount").GetInt32()),
            () => Assert.Equal(2, root.GetProperty("metrics").GetProperty("top1Count").GetInt32()),
            () => Assert.Empty(root.GetProperty("misses").EnumerateArray()));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":2,\"diagnosticLimit\":20,\"thresholds\":{},\"switchCriteria\":{},\"cases\":[]}")]
    [InlineData("{\"schemaVersion\":1,\"diagnosticLimit\":2,\"thresholds\":{},\"switchCriteria\":{},\"cases\":[]}")]
    [InlineData("{\"schemaVersion\":1,\"diagnosticLimit\":20,\"thresholds\":{},\"switchCriteria\":{},\"cases\":[{\"id\":\"\",\"query\":\"q\",\"expectedPaths\":[\"a\"]}]}")]
    [InlineData("{\"schemaVersion\":1,\"diagnosticLimit\":20,\"thresholds\":{},\"switchCriteria\":{},\"cases\":[{\"id\":\"same\",\"query\":\"q\",\"expectedPaths\":[\"a\"]},{\"id\":\"same\",\"query\":\"q\",\"expectedPaths\":[\"b\"]}]}")]
    public async Task WikiContextSearchEvaluationRunner_WithInvalidCorpus_Throws(string corpus) {
        string corpusPath = await WriteCorpusAsync(corpus);

        try {
            await Assert.ThrowsAsync<InvalidDataException>(() => WikiContextSearchEvaluationRunner.RunAsync(
                new SequencedContextSearch([]), corpusPath, TextWriter.Null, CancellationToken.None));
        } finally {
            File.Delete(corpusPath);
        }
    }

    [Fact]
    public async Task DevelopmentContextEvaluationRunner_WithCompleteContext_WritesPassingEvaluation() {
        string corpusPath = await WriteCorpusAsync("""
            {
              "schemaVersion": 1,
              "thresholds": {
                "minimumSqlitePrimaryRate": 1,
                "minimumScopeRecallRate": 1,
                "minimumSqlTopTenRecallRate": 1,
                "minimumCompleteBundleRate": 1,
                "minimumFocusedChecksRate": 1,
                "minimumExpectedLayersRate": 1,
                "maximumAverageExpandedScopePaths": 10,
                "maximumP95DurationMilliseconds": 10000,
                "maximumCompactCharacters": 10000
              },
              "cases": [{
                "id": "users",
                "intent": "Change users",
                "query": "user handler",
                "plannedPath": "FoodDiary.Application.Users",
                "expectedPaths": ["FoodDiary.Application.Users/Handler.cs"],
                "expectedLayers": ["Application"]
              }]
            }
            """);
        IWikiCommandExecutor executor = Substitute.For<IWikiCommandExecutor>();
        executor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(CreateCommandResult(call.ArgAt<string>(0))));
        IChangeSetSnapshotService snapshots = Substitute.For<IChangeSetSnapshotService>();
        var snapshot = new ChangeSetSnapshot("head", "fingerprint", [], DateTimeOffset.UtcNow);
        snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        var search = new SequencedContextSearch([CreateSearchResult([
            new WikiContextSearchCandidate(1, "FoodDiary.Application.Users/Handler.cs", "symbol", "Application", 100, 1, ["path/title affinity user handler"]),
        ])]);
        var queries = new WikiQueryService(executor, snapshots, contextSearch: search);
        await using var output = new StringWriter();

        try {
            await DevelopmentContextEvaluationRunner.RunAsync(queries, corpusPath, output, CancellationToken.None);
        } finally {
            File.Delete(corpusPath);
        }

        using var document = JsonDocument.Parse(output.ToString());
        JsonElement root = document.RootElement;
        Assert.Multiple(
            () => Assert.True(root.GetProperty("passed").GetBoolean()),
            () => Assert.Equal(1, root.GetProperty("caseCount").GetInt32()),
            () => Assert.Equal(1, root.GetProperty("metrics").GetProperty("sqlitePrimaryRate").GetDouble()),
            () => Assert.Equal(1, root.GetProperty("metrics").GetProperty("explainableRankingRate").GetDouble()),
            () => Assert.Equal(1, root.GetProperty("metrics").GetProperty("contextBundleReadyRate").GetDouble()),
            () => Assert.Equal(0, root.GetProperty("metrics").GetProperty("unplannedQueryRate").GetDouble()),
            () => Assert.Empty(root.GetProperty("failures").EnumerateArray()));
    }

    [Fact]
    public async Task DevelopmentContextEvaluationRunner_WithNoCandidates_DoesNotTreatRankingAsExplainable() {
        string corpusPath = await WriteCorpusAsync("""
            {
              "schemaVersion": 1,
              "thresholds": {
                "minimumSqlitePrimaryRate": 0,
                "minimumScopeRecallRate": 0,
                "minimumSqlTopTenRecallRate": 0,
                "minimumCompleteBundleRate": 0,
                "minimumFocusedChecksRate": 0,
                "minimumExpectedLayersRate": 0,
                "minimumExplainableRankingRate": 0,
                "minimumContextBundleReadyRate": 0,
                "maximumAverageExpandedScopePaths": 10,
                "maximumP95DurationMilliseconds": 10000,
                "maximumCompactCharacters": 10000
              },
              "cases": [{
                "id": "empty",
                "intent": "Inspect users",
                "query": "missing context",
                "plannedPath": "FoodDiary.Application.Users",
                "expectedPaths": ["FoodDiary.Application.Users/Missing.cs"],
                "expectedLayers": []
              }]
            }
            """);
        IWikiCommandExecutor executor = Substitute.For<IWikiCommandExecutor>();
        executor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(CreateCommandResult(call.ArgAt<string>(0))));
        IChangeSetSnapshotService snapshots = Substitute.For<IChangeSetSnapshotService>();
        var snapshot = new ChangeSetSnapshot("head", "fingerprint", [], DateTimeOffset.UtcNow);
        snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        var search = new SequencedContextSearch([CreateSearchResult([]), CreateSearchResult([])]);
        var queries = new WikiQueryService(executor, snapshots, contextSearch: search);
        await using var output = new StringWriter();

        try {
            await DevelopmentContextEvaluationRunner.RunAsync(queries, corpusPath, output, CancellationToken.None);
        } finally {
            File.Delete(corpusPath);
        }

        using var document = JsonDocument.Parse(output.ToString());
        JsonElement root = document.RootElement;
        Assert.Multiple(
            () => Assert.Equal(0, root.GetProperty("metrics").GetProperty("explainableRankingRate").GetDouble()),
            () => Assert.Equal(0, root.GetProperty("metrics").GetProperty("contextBundleReadyRate").GetDouble()),
            () => Assert.Single(root.GetProperty("failures").EnumerateArray()));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":2,\"thresholds\":{},\"cases\":[]}")]
    [InlineData("{\"schemaVersion\":1,\"thresholds\":{},\"cases\":[]}")]
    [InlineData("{\"schemaVersion\":1,\"thresholds\":{},\"cases\":[{\"id\":\"\",\"intent\":\"i\",\"query\":\"q\",\"plannedPath\":\"p\",\"expectedPaths\":[\"p\"],\"expectedLayers\":[]}]}")]
    [InlineData("{\"schemaVersion\":1,\"thresholds\":{},\"cases\":[{\"id\":\"same\",\"intent\":\"i\",\"query\":\"q\",\"plannedPath\":\"p\",\"expectedPaths\":[\"p\"],\"expectedLayers\":[]},{\"id\":\"same\",\"intent\":\"i\",\"query\":\"q\",\"plannedPath\":\"p\",\"expectedPaths\":[\"p\"],\"expectedLayers\":[]}]}")]
    public async Task DevelopmentContextEvaluationRunner_WithInvalidCorpus_Throws(string corpus) {
        string corpusPath = await WriteCorpusAsync(corpus);
        var queries = new WikiQueryService(
            Substitute.For<IWikiCommandExecutor>(),
            Substitute.For<IChangeSetSnapshotService>());

        try {
            await Assert.ThrowsAsync<InvalidDataException>(() => DevelopmentContextEvaluationRunner.RunAsync(
                queries, corpusPath, TextWriter.Null, CancellationToken.None));
        } finally {
            File.Delete(corpusPath);
        }
    }

    [Fact]
    public async Task ContextRoutingRetirementEvaluationRunner_WithPrimaryCompleteHit_RecordsEvidence() {
        string corpusPath = await WriteCorpusAsync("""
            {
              "schemaVersion": 1,
              "cases": [{
                "id": "users",
                "query": "user handler",
                "expectedPaths": ["FoodDiary.Application.Users/Handler.cs"]
              }]
            }
            """);
        string telemetryDirectory = Path.Combine(
            Path.GetTempPath(),
            "fooddiary-mcp-evaluation",
            Guid.NewGuid().ToString("N"));
        string telemetryPath = Path.Combine(telemetryDirectory, "routing.json");
        IWikiCommandExecutor executor = Substitute.For<IWikiCommandExecutor>();
        executor.ExecuteAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(CreateCommandResult(call.ArgAt<string>(0))));
        IChangeSetSnapshotService snapshots = Substitute.For<IChangeSetSnapshotService>();
        var snapshot = new ChangeSetSnapshot("head", "fingerprint", [], DateTimeOffset.UtcNow);
        snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        var search = new SequencedContextSearch([CreateSearchResult([
            new WikiContextSearchCandidate(1, "FoodDiary.Application.Users/Handler.cs", "symbol", "Application", 100, 1, []),
        ])]);
        ContextRoutingTelemetryStore routingStore = new(telemetryPath);
        WikiRuntimeTelemetry telemetry = new(routingStore);
        var queries = new WikiQueryService(executor, snapshots, contextSearch: search, telemetry: telemetry);
        await using var output = new StringWriter();

        try {
            await ContextRoutingRetirementEvaluationRunner.RunAsync(
                queries,
                routingStore,
                corpusPath,
                output,
                CancellationToken.None);

            using var document = JsonDocument.Parse(output.ToString());
            JsonElement root = document.RootElement;
            Assert.Multiple(
                () => Assert.True(root.GetProperty("passed").GetBoolean()),
                () => Assert.Equal(1, root.GetProperty("caseCount").GetInt32()),
                () => Assert.Equal(1, root.GetProperty("metrics").GetProperty("sqlitePrimaryCount").GetInt32()),
                () => Assert.Equal(1, root.GetProperty("persistentEvidenceAfter").GetProperty("sampleCount").GetInt32()));
        } finally {
            File.Delete(corpusPath);
            if (Directory.Exists(telemetryDirectory)) {
                Directory.Delete(telemetryDirectory, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":2,\"cases\":[]}")]
    [InlineData("{\"schemaVersion\":1,\"cases\":[]}")]
    [InlineData("{\"schemaVersion\":1,\"cases\":[{\"id\":\"\",\"query\":\"q\",\"expectedPaths\":[\"p\"]}]}")]
    [InlineData("{\"schemaVersion\":1,\"cases\":[{\"id\":\"same\",\"query\":\"q\",\"expectedPaths\":[\"p\"]},{\"id\":\"same\",\"query\":\"q\",\"expectedPaths\":[\"p\"]}]}")]
    public async Task ContextRoutingRetirementEvaluationRunner_WithInvalidCorpus_Throws(string corpus) {
        string corpusPath = await WriteCorpusAsync(corpus);
        string telemetryPath = Path.Combine(
            Path.GetTempPath(),
            $"fooddiary-mcp-routing-{Guid.NewGuid():N}.json");
        var queries = new WikiQueryService(
            Substitute.For<IWikiCommandExecutor>(),
            Substitute.For<IChangeSetSnapshotService>());

        try {
            await Assert.ThrowsAsync<InvalidDataException>(() => ContextRoutingRetirementEvaluationRunner.RunAsync(
                queries,
                new ContextRoutingTelemetryStore(telemetryPath),
                corpusPath,
                TextWriter.Null,
                CancellationToken.None));
        } finally {
            File.Delete(corpusPath);
            File.Delete(telemetryPath);
        }
    }

    private static WikiCommandResult CreateCommandResult(string command) {
        JsonElement structured = string.Equals(command, "test-plan", StringComparison.Ordinal)
            ? JsonSerializer.SerializeToElement(new { commands = new[] { "dotnet test focused" } })
            : JsonSerializer.SerializeToElement(new { change = new { paths = new[] { "FoodDiary.Application.Users/Handler.cs" } } });
        return new WikiCommandResult(command, "raw", structured, "repository", "head", [], [], ["dotnet test focused"], []);
    }

    private static WikiContextSearchResult CreateSearchResult(IReadOnlyList<WikiContextSearchCandidate> candidates) =>
        new("wiki", "sqlite", Ready: true, 10, "index", "2026-08-22T00:00:00Z", "fingerprint", "head", Fresh: true, ["query"], candidates, 1.5);

    private static async Task<string> WriteCorpusAsync(string corpus) {
        string path = Path.Combine(Path.GetTempPath(), $"fooddiary-mcp-evaluation-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, corpus);
        return path;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SequencedContextSearch(Queue<WikiContextSearchResult> results) : IWikiContextSearch {
        public SequencedContextSearch(IEnumerable<WikiContextSearchResult> results) : this(new Queue<WikiContextSearchResult>(results)) {
        }

        public Task<WikiContextSearchResult> SearchAsync(
            string query,
            int limit,
            string changeType,
            string? module,
            IReadOnlyList<string>? scopePaths,
            CancellationToken cancellationToken,
            string? expectedChangeSetFingerprint = null) =>
            Task.FromResult(results.Dequeue());
    }
}
