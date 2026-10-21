using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class SqliteFlowScopeReaderTests : IDisposable {
    [Fact]
    public async Task ExpandAsync_WithAlreadyVisitedSeeds_DoesNotRepeatRelationshipLinks() {
        DevelopmentFlowScope result = await new SqliteFlowScopeReader(_root).ExpandAsync(
            ["Modules/Inventory/Application/Commands/ReserveStock/ReserveStockCommandHandler.cs",
                "Modules/Inventory/Application/Services/IStockReservationService.cs",
                "Modules/Inventory/Application/Services/StockReservationService.cs"],
            includeTests: true, expectedFingerprint: "current", CancellationToken.None);

        Assert.True(result.Ready, result.UnavailableReason);
        Assert.NotEmpty(result.Links);
        Assert.Equal(result.Links.Count, result.Links.Distinct().Count());
        Assert.Contains("Modules/Inventory/Application/Services/StockReservationService.cs", result.Paths, StringComparer.Ordinal);
    }

    [Fact]
    public async Task ExpandAsync_WithMultipleHighFanOutSeeds_StopsAtTheGlobalLinkLimit() {
        await using (SqliteConnection connection = new($"Data Source={_databasePath};Pooling=False")) {
            await connection.OpenAsync();
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO files VALUES (100,'Modules/Inventory/Application/Services/IBoundedReservationService.cs');
                INSERT INTO symbols VALUES (100,'IBoundedReservationService','interface');
                WITH RECURSIVE items(n) AS (VALUES(110) UNION ALL SELECT n+1 FROM items WHERE n<169)
                INSERT INTO files SELECT n, 'Modules/Inventory/Application/Services/InventoryImplementation' || n || '.cs' FROM items;
                INSERT INTO typed_edges SELECT id, 'type-implementation', 'IBoundedReservationService' FROM files WHERE id>=110;
                INSERT INTO file_tokens VALUES (1,'IBoundedReservationService'),(4,'IBoundedReservationService'),(6,'IBoundedReservationService');
                """;
            await command.ExecuteNonQueryAsync();
        }

        DevelopmentFlowScope result = await new SqliteFlowScopeReader(_root).ExpandAsync(
            ["Modules/Inventory/Application/Commands/ReserveStock/ReserveStockCommandHandler.cs",
                "FoodDiary.JobManager/Services/StockReservationWorker.cs", "Other/UnrelatedHandler.cs"],
            includeTests: true, expectedFingerprint: "current", CancellationToken.None);

        Assert.True(result.Ready, result.UnavailableReason);
        Assert.True(result.Truncated);
        Assert.Equal(80, result.Links.Count);
        Assert.DoesNotContain(result.Links, link => string.Equals(link.SourcePath, "Other/UnrelatedHandler.cs", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Modules/Inventory/Application/Services/IStockReservationService.cs", "contract")]
    [InlineData("Modules\\Inventory\\Application\\Services\\IStockReservationService.cs", "contract")]
    [InlineData("Modules/Inventory/Application/Services/IStockReservationWorker.cs", "contract")]
    [InlineData("Modules/Inventory/Presentation/Services/StockNotifier.cs", "contract")]
    [InlineData("Modules/Inventory/Presentation/Controllers/StockController.cs", "endpoint")]
    [InlineData("FoodDiary.ReadModel.Composition/Dashboard/Reader.cs", "infrastructure")]
    public void Stage_DistinguishesContractsFromExecutionStagesAcrossPathSeparators(string path, string expected) =>
        Assert.Equal(expected, SqliteFlowScopeReader.Stage(path));

    private readonly string _root = Path.Combine(Path.GetTempPath(), "fooddiary-flow-" + Guid.NewGuid().ToString("N"));
    private readonly string _databasePath;

    public SqliteFlowScopeReaderTests() {
        _databasePath = Path.Combine(_root, ".artifacts", "llm-wiki", "code-graph", "code-graph.sqlite");
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        using SqliteConnection connection = new($"Data Source={_databasePath};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE metadata(key TEXT PRIMARY KEY, value TEXT);
            INSERT INTO metadata VALUES ('change_set_fingerprint','current');
            CREATE TABLE files(id INTEGER PRIMARY KEY, path TEXT UNIQUE);
            CREATE TABLE symbols(file_id INTEGER, name TEXT, kind TEXT);
            CREATE TABLE file_tokens(file_id INTEGER, token TEXT);
            CREATE TABLE typed_edges(file_id INTEGER, kind TEXT, target TEXT);
            INSERT INTO files VALUES
                (1,'Modules/Inventory/Application/Commands/ReserveStock/ReserveStockCommandHandler.cs'),
                (2,'Modules/Inventory/Application/Services/IStockReservationService.cs'),
                (3,'Modules/Inventory/Application/Services/StockReservationService.cs'),
                (4,'FoodDiary.JobManager/Services/StockReservationWorker.cs'),
                (5,'Modules/Inventory/tests/StockReservationTests.cs'),
                (6,'Other/UnrelatedHandler.cs');
            INSERT INTO symbols VALUES
                (1,'ReserveStockCommandHandler','class'), (2,'IStockReservationService','interface'),
                (3,'StockReservationService','class'), (4,'StockReservationWorker','class');
            INSERT INTO file_tokens VALUES (1,'IStockReservationService'),(5,'StockReservationService');
            INSERT INTO typed_edges VALUES
                (1,'mediator-handler','ReserveStockCommand'),(3,'type-inheritance','IStockReservationService'),
                (4,'mediator-dispatch','ReserveStockCommand'),(6,'method-call','Handle');
            """;
        command.ExecuteNonQuery();
    }

    [Fact]
    public async Task ExpandAsync_FollowsServiceImplementationAndWorkerWithoutUnrelatedMethodNames() {
        DevelopmentFlowScope result = await new SqliteFlowScopeReader(_root).ExpandAsync(
            ["Modules/Inventory/Application/Commands/ReserveStock/ReserveStockCommandHandler.cs"],
            includeTests: true, expectedFingerprint: "current", CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.Ready, result.UnavailableReason),
            () => Assert.Contains("Modules/Inventory/Application/Services/StockReservationService.cs", result.Paths, StringComparer.Ordinal),
            () => Assert.Contains("FoodDiary.JobManager/Services/StockReservationWorker.cs", result.Paths, StringComparer.Ordinal),
            () => Assert.Contains("Modules/Inventory/tests/StockReservationTests.cs", result.Paths, StringComparer.Ordinal),
            () => Assert.DoesNotContain("Other/UnrelatedHandler.cs", result.Paths, StringComparer.Ordinal),
            () => Assert.Contains(result.Links, link => string.Equals(link.Kind, "mediator-dispatch", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ExpandAsync_ExcludesTestsUnlessRequested() {
        DevelopmentFlowScope result = await new SqliteFlowScopeReader(_root).ExpandAsync(
            ["Modules/Inventory/Application/Services/StockReservationService.cs"],
            includeTests: false, expectedFingerprint: "current", CancellationToken.None);
        Assert.True(result.Ready, result.UnavailableReason);
        Assert.DoesNotContain(result.Paths, path => path.Contains("/tests/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExpandAsync_BoundsHighFanOutAndReportsTruncation() {
        await using (SqliteConnection connection = new($"Data Source={_databasePath};Pooling=False")) {
            await connection.OpenAsync();
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                WITH RECURSIVE items(n) AS (VALUES(10) UNION ALL SELECT n+1 FROM items WHERE n<80)
                INSERT INTO files SELECT n, 'Modules/Inventory/Application/Services/StockDependency' || n || '.cs' FROM items;
                INSERT INTO symbols SELECT id, 'StockDependency' || id, 'class' FROM files WHERE id>=10;
                INSERT INTO file_tokens SELECT 1, name FROM symbols WHERE file_id>=10;
                """;
            await command.ExecuteNonQueryAsync();
        }
        DevelopmentFlowScope result = await new SqliteFlowScopeReader(_root).ExpandAsync(
            ["Modules/Inventory/Application/Commands/ReserveStock/ReserveStockCommandHandler.cs"],
            includeTests: true, expectedFingerprint: "current", CancellationToken.None);
        Assert.Multiple(() => Assert.True(result.Ready, result.UnavailableReason), () => Assert.True(result.Truncated),
            () => Assert.InRange(result.Links.Count, 1, 80));
    }

    [Fact]
    public async Task ExpandAsync_MissingGraphTablesRemainUnavailable() {
        await using (SqliteConnection connection = new($"Data Source={_databasePath};Pooling=False")) {
            await connection.OpenAsync();
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "DROP TABLE typed_edges;";
            await command.ExecuteNonQueryAsync();
        }
        DevelopmentFlowScope result = await new SqliteFlowScopeReader(_root).ExpandAsync(
            ["Modules/Inventory/Application/Commands/ReserveStock/ReserveStockCommandHandler.cs"],
            includeTests: true, expectedFingerprint: "current", CancellationToken.None);
        Assert.Multiple(() => Assert.False(result.Ready), () => Assert.Empty(result.Paths),
            () => Assert.Equal("sqlite-error-1", result.UnavailableReason));
    }

    [Fact]
    public async Task ExpandAsync_RejectsStaleProjectionWithoutReturningLinks() {
        DevelopmentFlowScope result = await new SqliteFlowScopeReader(_root).ExpandAsync(
            ["FoodDiary.JobManager/Services/StockReservationWorker.cs"],
            includeTests: true, expectedFingerprint: "changed", CancellationToken.None);
        Assert.Multiple(() => Assert.False(result.Ready), () => Assert.Empty(result.Paths),
            () => Assert.Empty(result.Links), () => Assert.Equal("snapshot-mismatch", result.UnavailableReason));
    }

    [Fact]
    public async Task ExpandAsync_PropagatesCancellation() {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SqliteFlowScopeReader(_root).ExpandAsync(
            [], includeTests: true, expectedFingerprint: "current", cancellation.Token));
    }

    [Fact]
    public async Task ExpandAsync_MissingDatabaseStaysReadOnly() {
        string absentRoot = Path.Combine(_root, "absent");
        DevelopmentFlowScope result = await new SqliteFlowScopeReader(absentRoot).ExpandAsync(
            [], includeTests: true, expectedFingerprint: "current", CancellationToken.None);
        Assert.Multiple(() => Assert.False(result.Ready),
            () => Assert.Equal("database-missing", result.UnavailableReason), () => Assert.False(Directory.Exists(absentRoot)));
    }

    public void Dispose() {
        if (Directory.Exists(_root)) { Directory.Delete(_root, recursive: true); }
    }
}
