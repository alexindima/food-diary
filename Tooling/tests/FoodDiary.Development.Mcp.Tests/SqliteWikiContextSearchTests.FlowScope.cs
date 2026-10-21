namespace FoodDiary.Development.Mcp.Tests;

public sealed partial class SqliteWikiContextSearchTests {
    [Fact]
    public async Task EvaluationRunner_UsesTheSqliteBatchForAllCorpusCases() {
        CreateFlowSearchFixture();
        string corpus = Path.Combine(_fixtureRoot, "flow-evaluation.json");
        await File.WriteAllTextAsync(corpus, """
            {"schemaVersion":1,"diagnosticLimit":10,
             "thresholds":{"minimumCaseCount":1,"minimumTop1Rate":0,"minimumTop10Rate":1,"minimumMeanReciprocalRank":0},
             "switchCriteria":{"minimumCaseCount":1,"minimumTop1Rate":0,"minimumTop10Rate":1,"minimumMeanReciprocalRank":0},
             "cases":[{"id":"stock","query":"stock update","changeType":"Backend",
               "expectedPaths":["Modules/Inventory/Application/Commands/UpdateStock/UpdateStockCommandHandler.cs"]}]}
            """);
        await using var output = new StringWriter();

        await WikiContextSearchEvaluationRunner.RunAsync(new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()),
            corpus, output, CancellationToken.None, "fixture-change-set");

        using var document = System.Text.Json.JsonDocument.Parse(output.ToString());
        Assert.True(document.RootElement.GetProperty("passed").GetBoolean(), output.ToString());
    }

    [Theory]
    [InlineData("Trace stock updates from frontend to backend", false)]
    [InlineData("Trace starting the stock background job with tests", true)]
    [InlineData("Проследи обновление stock", false)]
    [InlineData("Проследи запуск фонового stock задания с тестами", true)]
    [InlineData("Trace the stock flow", false)]
    public async Task ExpandFlowScopeAsync_CombinesSearchAndGraphEvidence(string query, bool includeTests) {
        CreateFlowSearchFixture();
        var telemetry = new WikiRuntimeTelemetry();
        DevelopmentFlowScope result = await new SqliteWikiContextSearch(_fixtureRoot, telemetry).ExpandFlowScopeAsync(
            query, [StockFrontend], ["stock"], includeTests, "fixture-change-set", CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.Ready, result.UnavailableReason),
            () => Assert.Contains(StockFrontend, result.Paths, StringComparer.Ordinal),
            () => Assert.Contains(StockHandler, result.Paths, StringComparer.Ordinal),
            () => Assert.Contains(StockEndpoint, result.Paths, StringComparer.Ordinal),
            () => Assert.Contains(StockService, result.Paths, StringComparer.Ordinal),
            () => Assert.Contains(StockRepository, result.Paths, StringComparer.Ordinal),
            () => Assert.DoesNotContain("frontend", result.MissingStages, StringComparer.Ordinal),
            () => Assert.DoesNotContain("handler", result.MissingStages, StringComparer.Ordinal),
            () => Assert.Contains("do not prove a complete runtime chain", result.Interpretation, StringComparison.Ordinal),
            () => Assert.InRange(result.Paths.Count, 1, includeTests ? 12 : 10));
        Assert.NotEmpty(result.Links);
        Assert.All(result.Links, link => Assert.Contains(link.TargetPath, result.Paths, StringComparer.Ordinal));
        if (includeTests) {
            Assert.Contains(StockTests, result.Paths, StringComparer.Ordinal);
            Assert.Contains(StockWorker, result.Paths, StringComparer.Ordinal);
            Assert.DoesNotContain("tests", result.MissingStages, StringComparer.Ordinal);
            Assert.DoesNotContain("worker", result.MissingStages, StringComparer.Ordinal);
        } else {
            Assert.DoesNotContain(StockTests, result.Paths, StringComparer.Ordinal);
        }
    }

    [Theory]
    [InlineData("snapshot", "anchor-search-unavailable")]
    [InlineData("search", "anchor-search-unavailable")]
    [InlineData("graph", "sqlite-error-1")]
    public async Task ExpandFlowScopeAsync_UnavailableEvidenceDoesNotProduceAReadyFlow(string failure, string reason) {
        CreateFlowSearchFixture();
        if (string.Equals(failure, "search", StringComparison.Ordinal)) {
            ExecuteBatchFixtureSql("DELETE FROM context_search;");
        } else if (string.Equals(failure, "graph", StringComparison.Ordinal)) {
            ExecuteBatchFixtureSql("DROP TABLE typed_edges;");
        }

        DevelopmentFlowScope result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).ExpandFlowScopeAsync(
            "Trace stock updates", [StockFrontend], ["stock"], includeTests: true,
            string.Equals(failure, "snapshot", StringComparison.Ordinal) ? "stale-snapshot" : "fixture-change-set", CancellationToken.None);

        Assert.Multiple(
            () => Assert.False(result.Ready),
            () => Assert.Equal(reason, result.UnavailableReason),
            () => Assert.Empty(result.Links),
            () => Assert.False(result.Truncated));
    }

    [Fact]
    public async Task ExpandFlowScopeAsync_IgnoresGeneratedAdminAndTestPathsWhenChoosingAnAnchor() {
        CreateFlowSearchFixture();
        DevelopmentFlowScope result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).ExpandFlowScopeAsync(
            "Trace stock", ["FoodDiary.Web.Client/src/generated/client.ts", "FoodDiary.Web.Client/projects/fooddiary-admin/controller.ts", StockTests, StockFrontend],
            ["stock"], includeTests: true, "fixture-change-set", CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.Ready, result.UnavailableReason),
            () => Assert.Contains(StockHandler, result.Paths, StringComparer.Ordinal),
            () => Assert.Equal(StockTests, result.Paths[0]),
            () => Assert.Equal(StockFrontend, result.Paths[1]));
    }

    [Fact]
    public async Task ExpandFlowScopeAsync_ReportsMissingStagesForAnIsolatedSeed() {
        CreateFlowSearchFixture();
        DevelopmentFlowScope result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).ExpandFlowScopeAsync(
            "Trace unknown", ["Modules/Unknown/Application/Services/IsolatedService.cs"], ["isolated"],
            includeTests: false, "fixture-change-set", CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.Ready, result.UnavailableReason),
            () => Assert.Single(result.Paths),
            () => Assert.Equal(new[] { "frontend", "endpoint", "handler", "infrastructure" }, result.MissingStages),
            () => Assert.Empty(result.Links));
    }

    [Fact]
    public async Task ExpandFlowScopeAsync_PropagatesCancellation() {
        CreateFlowSearchFixture();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).ExpandFlowScopeAsync(
                "Trace stock", [StockFrontend], ["stock"], includeTests: true, "fixture-change-set", cancellation.Token));
    }

    private const string StockFrontend = "FoodDiary.Web.Client/src/app/stock.service.ts";
    private const string StockEndpoint = "Modules/Inventory/Presentation/StockController.cs";
    private const string StockHandler = "Modules/Inventory/Application/Commands/UpdateStock/UpdateStockCommandHandler.cs";
    private const string StockService = "Modules/Inventory/Application/Services/StockReservationService.cs";
    private const string StockRepository = "Modules/Inventory/Infrastructure/StockRepository.cs";
    private const string StockWorker = "FoodDiary.JobManager/Services/StockReservationWorker.cs";
    private const string StockTests = "Modules/Inventory/tests/StockReservationTests.cs";

    private void CreateFlowSearchFixture() {
        ExecuteBatchFixtureSql("""
            DELETE FROM context_search;
            DELETE FROM context_search_identity;
            CREATE TABLE files(id INTEGER PRIMARY KEY, path TEXT UNIQUE);
            CREATE TABLE symbols(file_id INTEGER, name TEXT, kind TEXT);
            CREATE TABLE file_tokens(file_id INTEGER, token TEXT);
            CREATE TABLE typed_edges(file_id INTEGER, kind TEXT, target TEXT);
            INSERT INTO files VALUES
                (1,'FoodDiary.Web.Client/src/app/stock.service.ts'),
                (2,'Modules/Inventory/Presentation/StockController.cs'),
                (3,'Modules/Inventory/Application/Commands/UpdateStock/UpdateStockCommandHandler.cs'),
                (4,'Modules/Inventory/Application/Services/StockReservationService.cs'),
                (5,'Modules/Inventory/Infrastructure/StockRepository.cs'),
                (6,'FoodDiary.JobManager/Services/StockReservationWorker.cs'),
                (7,'Modules/Inventory/tests/StockReservationTests.cs');
            INSERT INTO symbols VALUES
                (1,'StockService','class'), (2,'StockController','class'), (3,'UpdateStockCommandHandler','class'),
                (4,'StockReservationService','class'), (5,'StockRepository','class'), (6,'StockReservationWorker','class');
            INSERT INTO file_tokens VALUES (3,'StockReservationService'), (4,'StockRepository'), (7,'StockReservationService');
            INSERT INTO typed_edges VALUES
                (1,'mediator-dispatch','UpdateStockCommand'), (2,'mediator-dispatch','UpdateStockCommand'),
                (3,'mediator-handler','UpdateStockCommand'), (6,'mediator-dispatch','UpdateStockCommand');
            INSERT INTO context_search
                SELECT 'code',path,path,path,CASE WHEN id=1 THEN 'typescript' ELSE 'csharp' END,
                    CASE WHEN id=1 THEN 'StockService' ELSE 'StockReservation' END,
                    'stock update start process handler service worker tests'
                FROM files;
            INSERT INTO context_search_identity(rowid,path,title) SELECT rowid,path,title FROM context_search;
            """);
    }
}
