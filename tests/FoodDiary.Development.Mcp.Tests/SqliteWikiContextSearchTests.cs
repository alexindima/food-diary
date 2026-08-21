using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class SqliteWikiContextSearchTests : IDisposable {
    private readonly string _fixtureRoot;
    private readonly string _databasePath;

    public SqliteWikiContextSearchTests() {
        _fixtureRoot = Path.Combine(
            Path.GetTempPath(),
            "fooddiary-mcp-sqlite-tests",
            Guid.NewGuid().ToString("N"));
        string policyDirectory = Path.Combine(_fixtureRoot, ".llm-wiki", "policies");
        string databaseDirectory = Path.Combine(
            _fixtureRoot,
            ".artifacts",
            "llm-wiki",
            "code-graph");
        Directory.CreateDirectory(policyDirectory);
        Directory.CreateDirectory(databaseDirectory);
        string sourcePolicy = Path.Combine(
            FoodDiary.Development.Mcp.Infrastructure.RepositoryRootResolver.Resolve(),
            ".llm-wiki",
            "policies",
            "context-search-ranking.json");
        File.Copy(
            sourcePolicy,
            Path.Combine(policyDirectory, "context-search-ranking.json"));
        _databasePath = Path.Combine(databaseDirectory, "code-graph.sqlite");
        CreateDatabase();
    }

    [Theory]
    [InlineData(
        "strip user identity from web API telemetry logs",
        "Backend",
        "FoodDiary.Web.Api/Extensions/TelemetryPrivacyProcessor.cs")]
    [InlineData(
        "search external USDA foods over HTTP provider",
        "Backend",
        "FoodDiary.Integrations/Services/UsdaFoodSearchService.cs")]
    [InlineData(
        "где вычищаем идентификаторы пользователя из логов API",
        "Backend",
        "FoodDiary.Web.Api/Extensions/TelemetryPrivacyProcessor.cs")]
    [InlineData(
        "какой код ходит во внешний сервис USDA за продуктами",
        "Backend",
        "FoodDiary.Integrations/Services/UsdaFoodSearchService.cs")]
    public async Task SearchAsync_AppliesSharedRankingPolicy(
        string query,
        string changeType,
        string expectedPath) {
        WikiRuntimeTelemetry telemetry = new();
        SqliteWikiContextSearch search = new(_fixtureRoot, telemetry);

        WikiContextSearchResult result = await search.SearchAsync(
            query,
            limit: 10,
            changeType,
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.True(result.Ready, result.UnavailableReason);
        Assert.True(result.Fresh);
        Assert.Equal("sqlite-derived", result.Authority);
        Assert.Equal("in-process-microsoft-data-sqlite", result.Reader);
        Assert.Equal("fixture-change-set", result.ChangeSetFingerprint);
        Assert.Equal("fixture-head", result.GitHead);
        Assert.Equal(expectedPath, Assert.Single(result.Candidates, candidate => candidate.Rank == 1).Path);
        Assert.True(result.QueryDurationMilliseconds >= 0);
        WikiCommandStageTiming timing = Assert.Single(telemetry.Capture(0).CommandStageTimings);
        Assert.Equal("context-search", timing.Command);
        Assert.Equal("in-process-sqlite", timing.Stage);
    }

    [Fact]
    public async Task SearchAsync_RanksPrimaryDeclarationBeforeCompanionFile() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "render a diary PDF document with the generator",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.True(result.Ready, result.UnavailableReason);
        Assert.Equal(
            "FoodDiary.Infrastructure/Services/DiaryPdf/DiaryPdfGenerator.cs",
            result.Candidates[0].Path);
    }

    [Fact]
    public async Task SearchAsync_RejectsAnIndexFromAnotherChangeSet() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "strip user identity from web API telemetry logs",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "newer-change-set");

        Assert.False(result.Ready);
        Assert.False(result.Fresh);
        Assert.Equal("snapshot-mismatch", result.UnavailableReason);
        Assert.Equal("fixture-change-set", result.ChangeSetFingerprint);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task SearchAsync_ReturnsUnavailableWhenDatabaseDoesNotExist() {
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "anything",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None);

        Assert.False(result.Ready);
        Assert.Equal("database-missing", result.UnavailableReason);
        Assert.Empty(result.Candidates);
    }

    public void Dispose() {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_fixtureRoot)) {
            Directory.Delete(_fixtureRoot, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    private void CreateDatabase() {
        using SqliteConnection connection = new($"Data Source={_databasePath}");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE metadata(key TEXT PRIMARY KEY, value TEXT NOT NULL);
            INSERT INTO metadata(key, value) VALUES
                ('context_search_fingerprint', 'fixture-fingerprint'),
                ('context_search_updated_at_utc', '2026-08-21T00:00:00Z'),
                ('change_set_fingerprint', 'fixture-change-set'),
                ('change_set_git_head', 'fixture-head');
            CREATE VIRTUAL TABLE context_search USING fts5(
                record_type UNINDEXED,
                record_key UNINDEXED,
                path,
                source_path UNINDEXED,
                category UNINDEXED,
                title,
                body,
                tokenize = 'unicode61 remove_diacritics 2'
            );
            INSERT INTO context_search VALUES
                ('code', 'logs', 'FoodDiary.Presentation.Api/Features/Logs/LogsController.cs', 'logs', 'csharp', 'LogsController', 'web API telemetry logs'),
                ('code', 'privacy', 'FoodDiary.Web.Api/Extensions/TelemetryPrivacyProcessor.cs', 'privacy', 'csharp', 'TelemetryPrivacyProcessor', 'Sanitize SensitiveTags privacy'),
                ('code', 'usda-query', 'FoodDiary.Application.Usda/Queries/SearchUsdaFoods/SearchUsdaFoodsQueryHandler.cs', 'usda-query', 'csharp', 'SearchUsdaFoodsQueryHandler', 'USDA foods search'),
                ('code', 'usda-provider', 'FoodDiary.Integrations/Services/UsdaFoodSearchService.cs', 'usda-provider', 'csharp', 'UsdaFoodSearchService', 'HttpClient external provider'),
                ('code', 'pdf-primary', 'FoodDiary.Infrastructure/Services/DiaryPdf/DiaryPdfGenerator.cs', 'pdf-primary', 'csharp', 'DiaryPdfGenerator', 'render diary PDF document generator'),
                ('code', 'pdf-helper', 'FoodDiary.Infrastructure/Services/DiaryPdf/DiaryPdfGenerator.ChartSvgRenderer.cs', 'pdf-helper', 'csharp', 'DiaryPdfGenerator ChartSvgRenderer', 'render diary PDF document generator');
            """;
        command.ExecuteNonQuery();
    }
}
