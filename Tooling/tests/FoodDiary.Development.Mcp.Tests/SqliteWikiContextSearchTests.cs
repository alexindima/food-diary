using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class SqliteWikiContextSearchTests : IDisposable {
    [Theory]
    [InlineData("Find the integration with GhostNutrition927 provider", true)]
    [InlineData("Find the integration with NutritionProvider provider", false)]
    public async Task SearchAsync_ReportsUnmatchedExplicitIdentifierWithoutHidingCandidates(string query, bool unmatched) {
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            INSERT INTO context_search VALUES
                ('code','provider','Area/NutritionProvider.cs','provider','csharp','NutritionProvider','integration nutrition provider');
            """;
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            query, 10, "Any", module: null, scopePaths: null, CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");
        WikiContextSearchCandidate candidate = Assert.Single(result.Candidates);
        Assert.Multiple(
            () => Assert.True(result.Ready),
            () => Assert.Equal(unmatched, string.Equals(candidate.AmbiguityReason, "unmatched-query-identifier", StringComparison.Ordinal)),
            () => Assert.Equal(unmatched ? "low" : "unknown", candidate.Confidence));
    }

    [Theory]
    [InlineData("Which failed browser HTTP calls are retried?", "retry")]
    [InlineData("Где сервер получает сводку веса пользователя?", "weight")]
    [InlineData("Where are the verified boundaries and identities?", "verify")]
    public async Task SearchAsync_NormalizesConversationalInflections(string query, string expectedTerm) {
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            query, 10, "Any", module: null, scopePaths: null, CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");
        Assert.Contains(expectedTerm, result.QueryTerms, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("Frontend", "client interface for dashboard layout service", false)]
    [InlineData("Backend", "client interface for dashboard layout service", true)]
    [InlineData("Frontend", "dashboard layout service", true)]
    public async Task SearchAsync_PreservesLeadingModuleButRespectsExplicitFrontendLayer(string changeType, string query, bool moduleAffinity) {
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            INSERT INTO context_search VALUES
                ('code','dashboard','Modules/Dashboard/Application/DashboardService.cs','dashboard','csharp','DashboardService','client interface dashboard layout service');
            """;
        await command.ExecuteNonQueryAsync();
        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            query, 10, changeType, module: null, scopePaths: null, CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");
        WikiContextSearchCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal(moduleAffinity, candidate.Reasons.Contains("exact module identity dashboard", StringComparer.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_RecallsLongIndexPageWithBoundedIdentityPoolAsync() {
        string policyPath = Path.Combine(_fixtureRoot, ".llm-wiki", "policies", "context-search-ranking.json");
        System.Text.Json.Nodes.JsonNode policy = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(policyPath))!;
        policy["candidatePoolLimit"] = 1;
        policy["identityCandidatePoolLimit"] = 1;
        await File.WriteAllTextAsync(policyPath, policy.ToJsonString());
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO context_search VALUES
                ('wiki-page', 'long-index', '.llm-wiki/workflows/index.md', 'long-index', 'markdown', 'Index', $body),
                ('wiki-page', 'noise', '.llm-wiki/workflows/noise.md', 'noise', 'markdown', 'Noise', 'indexes indexes');
            INSERT INTO context_search_identity(rowid, path, title)
                SELECT rowid, path, title FROM context_search WHERE record_key IN ('long-index', 'noise');
            """;
        command.Parameters.AddWithValue("$body", string.Concat(Enumerable.Repeat("unrelated detail ", 10000)));
        await command.ExecuteNonQueryAsync();
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());
        WikiContextSearchResult result = await search.SearchAsync("indexes", 10, "Any", module: null, scopePaths: null, CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");
        Assert.Contains(result.Candidates, candidate => string.Equals(candidate.Path, ".llm-wiki/workflows/index.md", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("stock mcp tests xx", false, false)]
    [InlineData("stock excluded tests xx", false, false)]
    [InlineData("stock tests xx", true, true)]
    public async Task SearchAsync_RespectsExplicitMcpAndExcludedBoostTerms(string query, bool identityExpected, bool pathExpected) {
        string policyPath = Path.Combine(_fixtureRoot, ".llm-wiki", "policies", "context-search-ranking.json");
        System.Text.Json.Nodes.JsonNode policy = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(policyPath))!;
        policy["identityBoosts"] = System.Text.Json.Nodes.JsonNode.Parse("""
            [{"id":"explicit-powershell-file-intent","queryTerms":["stock"],"minimumMatches":1,"identityTerms":["stock"],"minimumIdentityMatches":1,"score":300,"identityScope":"file","excludedQueryTerms":["excluded"]}]
            """);
        policy["pathBoosts"] = System.Text.Json.Nodes.JsonNode.Parse("""
            [{"id":"stock-path","queryTerms":["stock"],"minimumMatches":1,"pathPrefixes":["Tooling/"],"score":300,"excludedQueryTerms":["excluded","mcp"]}]
            """);
        // Keep the short term in the query so it cannot be mistaken for a test subject.
        policy["directFileNameAffinity"]!["minimumTermLength"] = 3;
        await File.WriteAllTextAsync(policyPath, policy.ToJsonString());
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            INSERT INTO context_search VALUES ('code','stock','Tooling/Stock.cs','stock','csharp','Stock','stock mcp excluded tests xx');
            """;
        await command.ExecuteNonQueryAsync();

        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            query, 10, "Tests", module: null, scopePaths: null, CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");

        WikiContextSearchCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal(identityExpected, candidate.Reasons.Contains("ranking policy explicit-powershell-file-intent", StringComparer.Ordinal));
        Assert.Equal(pathExpected, candidate.Reasons.Any(reason => reason.Contains("stock-path", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("markers")]
    [InlineData("roleTerms")]
    public async Task SearchAsync_AllowsDisabledNegatedRolePolicy(string emptyProperty) {
        string policyPath = Path.Combine(_fixtureRoot, ".llm-wiki", "policies", "context-search-ranking.json");
        System.Text.Json.Nodes.JsonNode policy = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(policyPath))!;
        policy["negatedRolePenalty"]![emptyProperty] = new System.Text.Json.Nodes.JsonArray();
        await File.WriteAllTextAsync(policyPath, policy.ToJsonString());

        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            "not validator lesson", 10, "Backend", module: null, scopePaths: null, CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");

        Assert.True(result.Ready);
        Assert.NotEmpty(result.Candidates);
        Assert.All(result.Candidates, candidate => Assert.DoesNotContain(candidate.Reasons, reason => reason.StartsWith("negated role penalty", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SearchAsync_ScopeReplacementPreservesSoleRepresentativesAndHandlesMissingScopes(bool missingScope) {
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            INSERT INTO context_search VALUES
              ('code','a1','Area/a/first.cs','a1','csharp','Probe','scopeprobe'),
              ('code','a2','Area/a/second.cs','a2','csharp','Probe','scopeprobe'),
              ('code','b','Area/b/only.cs','b','csharp','Probe','scopeprobe'),
              ('code','c','Area/c/only.cs','c','csharp','Probe','scopeprobe');
            """;
        await command.ExecuteNonQueryAsync();

        WikiContextSearchResult result = await new SqliteWikiContextSearch(_fixtureRoot, new WikiRuntimeTelemetry()).SearchAsync(
            "scopeprobe", 3, "Backend", module: null, scopePaths: ["Area/a", "Area/b", missingScope ? "Area/missing" : "Area/c"],
            CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Equal(3, result.Candidates.Count);
        Assert.Contains(result.Candidates, candidate => candidate.Path.StartsWith("Area/a/", StringComparison.Ordinal));
        Assert.Contains(result.Candidates, candidate => candidate.Path.StartsWith("Area/b/", StringComparison.Ordinal));
        if (!missingScope) { Assert.Contains(result.Candidates, candidate => candidate.Path.StartsWith("Area/c/", StringComparison.Ordinal)); }
        Assert.Equal([1, 2, 3], result.Candidates.Select(candidate => candidate.Rank));
    }
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

    [Fact]
    public async Task SearchAsync_IdentityScopedBoost_MatchesSymbolTitleOutsideFileName() {
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            INSERT INTO context_search VALUES
                ('code', 'registration', 'Modules/Notifications/Infrastructure/Composition.cs', 'registration',
                 'csharp', 'NotificationResourceRegistration', 'notification resource registration renderer');
            """;
        await command.ExecuteNonQueryAsync();
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "notification resource registration", limit: 10, changeType: "Backend", module: null,
            scopePaths: null, CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");

        Assert.True(result.Ready, result.UnavailableReason);
        WikiContextSearchCandidate candidate = Assert.Single(result.Candidates);
        Assert.Contains("ranking policy notification-resource-registration-role", candidate.Reasons, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Domain/Entities/Stock.cs", "domain entity stock", "structural role domain-entity-layer-role", true)]
    [InlineData("Modules/Inventory/Domain/Entities/Stock.cs", "domain entity stock", "structural role domain-entity-layer-role", true)]
    [InlineData("Modules\\Inventory\\Domain\\Entities\\Stock.cs", "domain entity stock", "structural role domain-entity-layer-role", true)]
    [InlineData("Modules/Inventory/Infrastructure/Model/StockReservation.cs", "entity stock reservation quota", "structural role persistence-reservation-entity-role", true)]
    [InlineData("Modules/Inventory/Infrastructure/Persistence/StockReservation.cs", "entity stock reservation quota", "structural role persistence-reservation-entity-role", true)]
    [InlineData("Modules/Inventory/Infrastructure/Services/StockReservation.cs", "entity stock reservation quota", "structural role persistence-reservation-entity-role", false)]
    [InlineData("Modules/Inventory/Application/InventoryReader.cs", "inventory reader", "exact module identity inventory", true)]
    [InlineData("Modules/InventoryExtras/Application/InventoryReader.cs", "inventory reader", "exact module identity inventory", false)]
    [InlineData("ModulesExtra/Inventory/Domain/Entities/Stock.cs", "domain entity stock", "structural role domain-entity-layer-role", false)]
    [InlineData("Modules/Inventory/Domain/tests/Stock.cs", "domain entity stock", "structural role domain-entity-layer-role", false)]
    [InlineData("Modules/Inventory/Application/Abstractions/Entities/Stock.cs", "domain entity stock", "structural role domain-entity-layer-role", false)]
    [InlineData("Modules/Inventory/Domain/Entities/Notifications/StockNotice.cs", "inventory user channel notification", "structural role notification-domain-entity-role", false)]
    [InlineData("Modules/Inventory/Domain/Entities/Notifications/StockNotice.cs", "inventory domain user channel notification", "structural role notification-domain-entity-role", true)]
    [InlineData("FoodDiary.Domain/Entities/Notifications/StockNotice.cs", "inventory user channel notification", "structural role notification-domain-entity-role", true)]
    [InlineData("Modules/Inventory/Infrastructure/R7Probe.cs", "r7", "direct file-name affinity r7", true)]
    [InlineData("Modules/Inventory/Infrastructure/Providers/Services/SupplierClient.cs", "external supplier client", "generic integration-layer affinity", true)]
    [InlineData("Modules\\Inventory\\Infrastructure\\Providers\\Services\\SupplierClient.cs", "external supplier client", "generic integration-layer affinity", true)]
    [InlineData("Modules/Inventory/Infrastructure/ProvidersExtra/SupplierClient.cs", "external supplier client", "generic integration-layer affinity", false)]
    [InlineData("Modules/Inventory/Infrastructure/Providers/tests/SupplierClient.cs", "external supplier client", "generic integration-layer affinity", false)]
    [InlineData("Shared/FoodDiary.Domain.Primitives/RequiredValue.cs", "domain required value", "generic domain-layer affinity", true)]
    [InlineData("Shared\\FoodDiary.Domain.Primitives\\RequiredValue.cs", "domain required value", "generic domain-layer affinity", true)]
    [InlineData("Shared/FoodDiary.Domain.PrimitivesExtra/RequiredValue.cs", "domain required value", "generic domain-layer affinity", false)]
    [InlineData("Shared/FoodDiary.Domain.Primitives/tests/RequiredValue.cs", "domain required value", "generic domain-layer affinity", false)]
    [InlineData("Shared/FoodDiary.Integrations.Http/Http/SampleTransport.cs", "external supplier client", "generic integration-layer affinity", true)]
    [InlineData("Shared\\FoodDiary.Integrations.Http\\Http\\SampleTransport.cs", "external supplier client", "generic integration-layer affinity", true)]
    [InlineData("Shared/FoodDiary.Integrations.HttpExtra/Http/SampleTransport.cs", "external supplier client", "generic integration-layer affinity", false)]
    [InlineData("Shared/FoodDiary.Integrations.Http/tests/SampleTransport.cs", "external supplier client", "generic integration-layer affinity", false)]
    [InlineData("Shared/FoodDiary.Inventory.PersistenceModel/StockRecord.cs", "stock storage implementation", "generic database-layer affinity", true)]
    [InlineData("FoodDiary.ReadModel.Composition/Inventory/StockReader.cs", "stock storage implementation", "generic database-layer affinity", true)]
    [InlineData("FoodDiary.ReadModel.Composition/Inventory/StockReader.cs", "inventory stock read", "exact module identity inventory", true)]
    [InlineData("Modules/Inventory/Infrastructure/ModuleRegistration.cs", "storage or provider implementation for dependency injection inventory", "exact module identity inventory", true)]
    [InlineData("Modules/Shipping/Infrastructure/ModuleRegistration.cs", "storage or provider implementation for dependency injection inventory", "exact module identity inventory", false)]
    [InlineData("Modules/Inventory/Infrastructure/ModuleRegistration.cs", "stock storage implementation inventory", "exact module identity inventory", false)]
    [InlineData("FoodDiary.ReadModel.CompositionExtra/Inventory/StockReader.cs", "stock storage implementation", "generic database-layer affinity", false)]
    [InlineData("FoodDiary.ReadModel.Composition/Inventory/tests/StockReader.cs", "stock storage implementation", "generic database-layer affinity", false)]
    [InlineData("Shared\\FoodDiary.Inventory.PersistenceModel\\StockRecord.cs", "stock storage implementation", "generic infrastructure-layer affinity", true)]
    [InlineData("Shared/FoodDiary.Inventory.PersistenceModel/Configurations/StockConfiguration.cs", "stock storage implementation", "generic database-layer affinity", true)]
    [InlineData("Shared/FoodDiary.Inventory.PersistenceModelExtra/StockRecord.cs", "stock storage implementation", "generic database-layer affinity", false)]
    [InlineData("Shared/FoodDiary.Inventory.PersistenceModel/tests/StockRecord.cs", "stock storage implementation", "generic database-layer affinity", false)]
    [InlineData("Shared/FoodDiary..PersistenceModel/StockRecord.cs", "stock storage implementation", "generic database-layer affinity", false)]
    [InlineData("Modules/Inventory/Infrastructure/Providers/Services/SupplierClient.cs", "stock provider implementation", "generic infrastructure-layer affinity", false)]
    [InlineData("Modules/Inventory/Infrastructure/Services/StockStore.cs", "stock provider implementation", "generic infrastructure-layer affinity", true)]
    [InlineData("FoodDiary.Integrations/Services/SupplierClient.cs", "stock provider implementation", "generic infrastructure-layer affinity", false)]
    [InlineData("Modules/Inventory/Presentation/Mappings/StockResponseMappings.cs", "stock confirm result response http mapping", "structural role api-response-mapping-role", true, "Api")]
    [InlineData("Modules\\Inventory\\Presentation\\Mappings\\StockResponseMappings.cs", "stock confirm result response http mapping", "structural role api-response-mapping-role", true, "Api")]
    [InlineData("Modules/Inventory/PresentationExtra/Mappings/StockResponseMappings.cs", "stock confirm result response http mapping", "structural role api-response-mapping-role", false, "Api")]
    [InlineData("Modules/Inventory/Contracts/Stock/IStockReadService.cs", "load stock read", "structural role backend-read-service-role", false)]
    [InlineData("Modules/Inventory/ContractsExtra/Stock/IStockReadService.cs", "load stock read", "structural role backend-read-service-role", true)]
    [InlineData("Modules/Inventory/Application/StockCount.cs", "stock-count", "direct file-name affinity stock, count, stockcount", true)]
    [InlineData("Modules/Inventory/Application/Ab.cs", "a-b", "direct file-name affinity ab", false)]
    [InlineData("Modules/Inventory/tests/Inventory.Tests/AppleTests.cs", "apples test", "direct file-name affinity apples", true, "Tests")]
    [InlineData("Modules/Inventory/tests/Inventory.Tests/AppleTests.cs", "pears test", "direct file-name affinity pears", false, "Tests")]
    [InlineData("Modules/Inventory/Application/Apple.cs", "apples test", "direct file-name affinity apples", false, "Tests")]
    [InlineData("Modules/Inventory/tests/Inventory.Tests/AppleTests.cs", "apples", "direct file-name affinity apples", false)]
    [InlineData("Modules/Inventory/Application/Abstractions/IStockService.cs", "inventory stock lookup", "module entry-point abstraction penalty waived", true)]
    [InlineData("Modules/Inventory/Application/Abstractions/IStockService.cs", "inventory external provider lookup", "module entry-point abstraction penalty waived", false)]
    [InlineData("Modules/Inventory/Application/Abstractions/IStockService.cs", "inventory http lookup", "module entry-point abstraction penalty waived", false)]
    [InlineData("Modules/Inventory/Application/Abstractions/IStockService.cs", "shipping stock lookup", "module entry-point abstraction penalty waived", false)]
    [InlineData("Modules/Inventory/Application/tests/IStockService.cs", "inventory stock lookup", "module entry-point abstraction penalty waived", false)]
    public async Task SearchAsync_RecognizesModuleLayerSelectors(
        string path,
        string query,
        string expectedReason,
        bool expectedMatch,
        string changeType = "Backend") {
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            INSERT INTO context_search VALUES
                ('code', 'layout-fixture', $path, $path, 'csharp', 'Stock Inventory Reader Reservation', $body);
            """;
        command.Parameters.AddWithValue("$path", path);
        command.Parameters.AddWithValue("$body", query);
        await command.ExecuteNonQueryAsync();
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            query, limit: 10, changeType: changeType, module: null, scopePaths: null, CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.True(result.Ready, result.UnavailableReason);
        WikiContextSearchCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal(path, candidate.Path);
        Assert.Equal(expectedMatch, candidate.Reasons.Any(reason =>
            reason.StartsWith(expectedReason, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("Modules/Inventory/tests/FoodDiary.Modules.Inventory.Infrastructure.IntegrationTests/StockStoreTests.cs", true)]
    [InlineData("Modules\\Shipping\\tests\\FoodDiary.Modules.Shipping.Infrastructure.IntegrationTests\\StockStoreTests.cs", true)]
    [InlineData("tests/FoodDiary.Infrastructure.IntegrationTests/StockStoreTests.cs", true)]
    [InlineData("Modules/Inventory/tests/FoodDiary.Modules.Shipping.Infrastructure.IntegrationTests/StockStoreTests.cs", false)]
    [InlineData("Modules/Inventory/tests/FoodDiary.Modules.Inventory.Infrastructure.IntegrationTestsExtra/StockStoreTests.cs", false)]
    [InlineData("Tooling/tests/FoodDiary.Analyzers.Tests/StockStoreTests.cs", true, "tests/FoodDiary.Analyzers.Tests/")]
    [InlineData("Shared/tests/FoodDiary.Domain.Primitives.Tests/StockStoreTests.cs", true, "tests/FoodDiary.Domain.Primitives.Tests/")]
    [InlineData("Shared/tests/Unrelated/StockStoreTests.cs", false, "tests/Unrelated/")]
    [InlineData("Shared/FoodDiary.Email.PersistenceModel/StockStoreTests.cs", true, "FoodDiary.Infrastructure/Persistence/Email/")]
    [InlineData("Shared/FoodDiary.Email.PersistenceModel/Configurations/StockStoreTests.cs", true, "FoodDiary.Infrastructure/Persistence/Configurations/Email/")]
    public async Task SearchAsync_PreservesIntegrationTestSelectorAfterModuleRelocation(string path, bool expectedMatch,
        string selectorPrefix = "tests/FoodDiary.Infrastructure.IntegrationTests/") {
        string policyPath = Path.Combine(_fixtureRoot, ".llm-wiki", "policies", "context-search-ranking.json");
        System.Text.Json.Nodes.JsonNode policy = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(policyPath))!;
        // A synthetic role tests selector equivalence without any benchmark vocabulary.
        policy["structuralRoleBoosts"] = System.Text.Json.Nodes.JsonNode.Parse("""
            [{"id":"synthetic-stock-integration-tests","queryTerms":["stock"],"minimumMatches":1,
              "candidateTerms":["stock"],"minimumCandidateMatches":1,"minimumQueryIdentityMatches":1,
              "score":300,"identityScope":"file","changeTypes":["Tests"],"recordTypes":["code"],
              "pathPrefixes":["tests/FoodDiary.Infrastructure.IntegrationTests/"],"pathSuffixes":["Tests.cs"]}]
            """);
        policy["structuralRoleBoosts"]![0]!["pathPrefixes"] = System.Text.Json.JsonSerializer.SerializeToNode<string[]>([selectorPrefix]);
        await File.WriteAllTextAsync(policyPath, policy.ToJsonString());
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM context_search;
            INSERT INTO context_search VALUES ('code', 'stock-fixture', $path, $path, 'csharp', 'Stock store tests', 'stock store integration tests');
            """;
        command.Parameters.AddWithValue("$path", path);
        await command.ExecuteNonQueryAsync();
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "stock store integration tests", limit: 10, changeType: "Tests", module: null, scopePaths: null,
            CancellationToken.None, expectedChangeSetFingerprint: "fixture-change-set");

        Assert.True(result.Ready, result.UnavailableReason);
        WikiContextSearchCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal(path, candidate.Path);
        Assert.Equal(expectedMatch, candidate.Reasons.Any(reason =>
            reason.StartsWith("structural role synthetic-stock-integration-tests (", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("Acme", "Tests", true)]
    [InlineData("Zephyr", "Tests", true)]
    [InlineData("Acme", "Backend", false)]
    public async Task SearchAsync_PrefersDirectTestSubjectOverGenericClientVocabulary(
        string subject, string changeType, bool expectsSpecificity) {
        await using SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using SqliteCommand clear = connection.CreateCommand();
        clear.CommandText = "DELETE FROM context_search;";
        await clear.ExecuteNonQueryAsync();
        // Keep the client/provider role equal; vary subject versus scaffolding only.
        string expectedPath = $"tests/Suite0/{subject}ProviderClientTests.cs";
        for (int index = 0; index < 16; index++) {
            await using SqliteCommand insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO context_search VALUES ('code', $key, $path, $path, 'csharp', 'Client tests', $body);";
            insert.Parameters.AddWithValue("$key", FormattableString.Invariant($"subject-{index}"));
            string genericPath = index == 1 ? "tests/Suite1/FeatureProviderClientTests.cs" : FormattableString.Invariant($"tests/Suite{index}/ProviderClientTests.cs");
            insert.Parameters.AddWithValue("$path", index == 0 ? expectedPath : genericPath);
            insert.Parameters.AddWithValue("$body", $"automated tests {subject} provider client");
            await insert.ExecuteNonQueryAsync();
        }
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());
        WikiContextSearchResult result = await search.SearchAsync(
            $"automated tests {subject} provider client feature", limit: 20, changeType,
            module: null, scopePaths: null, CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");
        Assert.True(result.Ready, result.UnavailableReason);
        WikiContextSearchCandidate subjectCandidate = Assert.Single(result.Candidates, candidate => string.Equals(candidate.Path, expectedPath, StringComparison.Ordinal));
        Assert.Equal(expectsSpecificity, subjectCandidate.Reasons.Contains("direct test-subject specificity 105", StringComparer.Ordinal));
        if (expectsSpecificity) {
            Assert.Equal(expectedPath, result.Candidates[0].Path);
            Assert.All(result.Candidates.Skip(1), candidate => Assert.DoesNotContain(
                candidate.Reasons, reason => reason.StartsWith("direct test-subject specificity", StringComparison.Ordinal)));
        }
    }

    [Fact]
    public void Constructor_UsesResolvedRepositoryRoot() {
        var search = new SqliteWikiContextSearch(new WikiRuntimeTelemetry());

        Assert.NotNull(search);
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
            "Modules/Export/Infrastructure/Services/DiaryPdf/DiaryPdfGenerator.cs",
            result.Candidates[0].Path);
    }

    [Fact]
    public async Task SearchAsync_AppliesGenericRoleAndLayerAffinities() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "service searches external USDA foods over HTTP provider",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        WikiContextSearchCandidate top = Assert.Single(result.Candidates, candidate => candidate.Rank == 1);
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Integrations/Services/UsdaFoodSearchService.cs", top.Path),
            () => Assert.Contains(top.Reasons, reason => reason.StartsWith("generic file-role affinity", StringComparison.Ordinal)),
            () => Assert.Contains(top.Reasons, reason => string.Equals(reason, "generic integration-layer affinity", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task SearchAsync_KeepsTopConfidenceStableAcrossVisibleLimits() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult limitOne = await search.SearchAsync(
            "strip user identity from web API telemetry logs",
            limit: 1,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");
        WikiContextSearchResult limitFive = await search.SearchAsync(
            "strip user identity from web API telemetry logs",
            limit: 5,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        WikiContextSearchCandidate first = Assert.Single(limitOne.Candidates);
        WikiContextSearchCandidate comparison = limitFive.Candidates[0];
        Assert.Multiple(
            () => Assert.Equal(comparison.Path, first.Path),
            () => Assert.Equal(comparison.ScoreMargin, first.ScoreMargin),
            () => Assert.Equal(comparison.Confidence, first.Confidence),
            () => Assert.Equal(comparison.Ambiguous, first.Ambiguous),
            () => Assert.Equal(comparison.SameNameCandidateCount, first.SameNameCandidateCount));
    }

    [Fact]
    public async Task SearchAsync_ReportsNameCollisionsWithoutCallingCandidatesEquivalent() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "program host startup",
            limit: 5,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        WikiContextSearchCandidate[] programs = [.. result.Candidates.Where(candidate =>
            candidate.Path.EndsWith("/Program.cs", StringComparison.Ordinal))];
        Assert.Equal(2, programs.Length);
        Assert.All(programs, candidate => Assert.Equal(2, candidate.SameNameCandidateCount));
        Assert.DoesNotContain(programs, candidate => candidate.AmbiguityReason?.Contains(
            "equivalent",
            StringComparison.OrdinalIgnoreCase) == true);
    }

    [Theory]
    [InlineData("coveragebranch domain entity", "Any", true)]
    [InlineData("coveragebranch api endpoint", "Any", true)]
    [InlineData("coveragebranch provider implementation", "Any", true)]
    [InlineData("coveragebranch database persistence", "Any", true)]
    [InlineData("coveragebranch domain guide", "Any", false)]
    [InlineData("coveragebranch provider documentation", "Any", false)]
    [InlineData("coveragebranch", "Any", false)]
    [InlineData("coveragebranch implementation", "Any", false)]
    [InlineData("coveragebranch", "Backend", true)]
    public async Task SearchAsync_UsesDeclaredImplementationIntentForDocumentationPenalty(
        string query,
        string changeType,
        bool expectedPenalty) {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());
        WikiContextSearchResult result = await search.SearchAsync(
            query,
            limit: 50,
            changeType: changeType,
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        WikiContextSearchCandidate guide = Assert.Single(result.Candidates, candidate =>
            string.Equals(candidate.RecordType, "agent-guide", StringComparison.Ordinal));
        Assert.Equal(expectedPenalty, guide.Reasons.Contains(
            "documentation candidate penalty for implementation intent", StringComparer.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_LowersConfidenceForDocumentationReturnedForImplementationChange() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "coveragebranch agent guide",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        WikiContextSearchCandidate guide = Assert.Single(result.Candidates, candidate =>
            string.Equals(candidate.RecordType, "agent-guide", StringComparison.Ordinal));
        Assert.Multiple(
            () => Assert.Equal("low", guide.Confidence),
            () => Assert.True(guide.Ambiguous),
            () => Assert.Equal("record-type-change-type-mismatch", guide.AmbiguityReason));
    }

    [Fact]
    public async Task SearchAsync_ExpandsCommonEnglishInflections() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "endpoints policies querying suppressed",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Contains("endpoint", result.QueryTerms, StringComparer.Ordinal);
        Assert.Contains("policy", result.QueryTerms, StringComparer.Ordinal);
        Assert.Contains("query", result.QueryTerms, StringComparer.Ordinal);
        Assert.Contains("suppress", result.QueryTerms, StringComparer.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_ExpandsDoubledConsonantInflection() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "running",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Contains("run", result.QueryTerms, StringComparer.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_ReturnsUnavailableWhenQueryContainsOnlyStopTerms() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "the and",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None);

        Assert.Equal("query-has-no-search-terms", result.UnavailableReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{ \"schemaVersion\": 2 }")]
    public async Task SearchAsync_ReturnsUnavailableForMissingOrInvalidRankingPolicy(string? policyContent) {
        string policyPath = Path.Combine(_fixtureRoot, ".llm-wiki", "policies", "context-search-ranking.json");
        if (policyContent is null) {
            File.Delete(policyPath);
        } else {
            await File.WriteAllTextAsync(policyPath, policyContent);
        }
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "anything",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None);

        Assert.Equal("context-search-configuration-unavailable", result.UnavailableReason);
    }

    [Fact]
    public async Task SearchAsync_ReturnsUnavailableWhenProjectionMetadataIsIncomplete() {
        await using (SqliteConnection connection = new($"Data Source={_databasePath}")) {
            await connection.OpenAsync();
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM metadata WHERE key = 'context_search_fingerprint';";
            await command.ExecuteNonQueryAsync();
        }
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "anything",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None);

        Assert.Equal("fts-projection-not-ready", result.UnavailableReason);
    }

    [Fact]
    public async Task SearchAsync_ScopesRoleBoostsToTheFileIdentity() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "delete expired user login events on a schedule",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Equal(
            "FoodDiary.JobManager/Services/UserLoginEventCleanupJob.cs",
            result.Candidates[0].Path);
    }

    [Fact]
    public async Task SearchAsync_DoesNotTreatIndexedTitleAsTheFileIdentity() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "database reader returns product overview information",
            limit: 10,
            changeType: "Database",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Equal(
            "FoodDiary.Infrastructure/Persistence/Products/ProductOverviewReadService.cs",
            result.Candidates[0].Path);
    }

    [Fact]
    public async Task SearchAsync_PrefersPowerShellFilesForExplicitPowerShellIntent() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "PowerShell tool finds sensitive domain data touched by a change",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Equal(
            ".llm-wiki/tools/Find-LlmWikiSensitiveData.ps1",
            result.Candidates[0].Path);
    }

    [Fact]
    public async Task SearchAsync_PrefersSpecificClientIdentityOverGenericApiClientRole() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "HTTP client sends food recognition requests to OpenAI",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Equal(
            "FoodDiary.Integrations/Services/OpenAi/OpenAiFoodClient.cs",
            result.Candidates[0].Path);
    }

    [Fact]
    public async Task SearchAsync_UsesFileSubjectToBreakTiesWithinMatchedRole() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "validate configured integration URLs before startup",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Equal(
            "FoodDiary.Integrations/Options/IntegrationUriValidator.cs",
            result.Candidates[0].Path);
        Assert.Contains("uri", result.QueryTerms, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.ConsentAndConfirmation.cs")]
    [InlineData("Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.ConsentAndConfirmation.cs")]
    public async Task SearchAsync_PrefersBehaviorSpecificPartialTestForExplicitTestIntent(string testPath) {
        await using (SqliteConnection connection = new($"Data Source={_databasePath}")) {
            await connection.OpenAsync();
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE context_search SET path=$path WHERE path=$old;";
            command.Parameters.AddWithValue("$path", testPath);
            command.Parameters.AddWithValue("$old", "tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.ConsentAndConfirmation.cs");
            await command.ExecuteNonQueryAsync();
        }
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "какие tests покрывают confirm period start и update cycle consent: owner, missing profile, invalid user и validator failures",
            limit: 10,
            changeType: "Tests",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Equal(
            testPath,
            result.Candidates[0].Path);
        Assert.Contains(
            result.Candidates[0].Reasons,
            reason => reason.StartsWith("explicit test behavior affinity", StringComparison.Ordinal));
        Assert.DoesNotContain(
            result.Candidates[0].Reasons,
            reason => string.Equals(reason, "companion file ranked after primary declaration", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_PenalizesExplicitlyNegatedNeighborRoles() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "не validator команды урока, а общий parser значений category и difficulty в enum с именем поля",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        WikiContextSearchCandidate parser = Assert.Single(
            result.Candidates,
            candidate => string.Equals(
                candidate.Path,
                "Modules/Admin/Application/Common/AdminLessonValueParser.cs",
                StringComparison.Ordinal));
        WikiContextSearchCandidate validator = Assert.Single(
            result.Candidates,
            candidate => string.Equals(
                candidate.Path,
                "Modules/Admin/Application/Commands/CreateAdminLesson/CreateAdminLessonCommandValidator.cs",
                StringComparison.Ordinal));
        Assert.True(parser.Rank < validator.Rank);
        Assert.Contains(
            validator.Reasons,
            reason => reason.StartsWith("negated role penalty", StringComparison.Ordinal));
        Assert.DoesNotContain("validator", result.QueryTerms, StringComparer.Ordinal);
        Assert.DoesNotContain("command", result.QueryTerms, StringComparer.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_UsesAStableCandidatePoolForEveryRequestedLimit() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult five = await search.SearchAsync(
            "coveragebranch",
            limit: 5,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");
        WikiContextSearchResult twenty = await search.SearchAsync(
            "coveragebranch",
            limit: 20,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Equal(
            five.Candidates.Select(candidate => candidate.Path),
            twenty.Candidates.Take(five.Candidates.Count).Select(candidate => candidate.Path),
            StringComparer.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_ExpandsRussianTechnicalVocabularyBeforeFts() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "валидатор настроенных URL интеграции, репозиторий идемпотентности и отчет покрытия",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Multiple(
            () => Assert.Contains("validator", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("configuration", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("repository", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("idempotency", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("coverage", result.QueryTerms, StringComparer.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_ExpandsRussianSubjectAndRoleVocabularyBeforeFts() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "безопасность изображений, любимый статус, подтверждение контракта и сравнение",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Multiple(
            () => Assert.Contains("security", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("image", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("favorite", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("status", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("verification", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("interface", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("comparison", result.QueryTerms, StringComparer.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_ExpandsEveryAlternativeInGroupedRussianPrefixes() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "избранный продукт и еженедельный результат",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Multiple(
            () => Assert.Contains("favorite", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("weekly", result.QueryTerms, StringComparer.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_ExpandsQualityAndInvariantVocabulary() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "builder качества еды, упражнение, инвариант, смена и уведомление",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Multiple(
            () => Assert.Contains("build", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("quality", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("grade", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("food", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("exercise", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("invariant", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("change", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("notification", result.QueryTerms, StringComparer.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_DoesNotTranslateRegistrationIntoAHostedService() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "контракт проверки регистрации recurring jobs",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Multiple(
            () => Assert.Contains("interface", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("verification", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("verifier", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.Contains("registration", result.QueryTerms, StringComparer.Ordinal),
            () => Assert.DoesNotContain("hostedservice", result.QueryTerms, StringComparer.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_DoesNotExpandANegatedRoleIntoItsNeighborRole() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "handler cleanup, not command DTO",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Contains("handler", result.QueryTerms, StringComparer.Ordinal);
        Assert.DoesNotContain("command", result.QueryTerms, StringComparer.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_TranslatesNegatedConfigurationIntoUnconfiguredIntent() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "хранилище картинок не настроено",
            limit: 10,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Contains("unconfigured", result.QueryTerms, StringComparer.Ordinal);
        Assert.DoesNotContain("configuration", result.QueryTerms, StringComparer.Ordinal);
        Assert.DoesNotContain("configured", result.QueryTerms, StringComparer.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_AppliesModuleScopeLayerAndTechnicalRankingBranches() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult scoped = await search.SearchAsync(
            "coveragebranch",
            limit: 20,
            changeType: "Frontend",
            module: "fooddiary.infrastructure",
            scopePaths: [" ", "FoodDiary.Infrastructure/Services"],
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");
        WikiContextSearchResult backend = await search.SearchAsync(
            "coveragebranch",
            limit: 20,
            changeType: "Backend",
            module: null,
            scopePaths: null,
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        WikiContextSearchCandidate exact = Assert.Single(
            scoped.Candidates,
            candidate => string.Equals(candidate.Path, "FoodDiary.Infrastructure/Services/CoverageBranch.cs", StringComparison.Ordinal));
        Assert.Multiple(
            () => Assert.Contains(exact.Reasons, reason => string.Equals(reason, "exact normalized query match", StringComparison.Ordinal)),
            () => Assert.Contains(exact.Reasons, reason => reason.StartsWith("module ", StringComparison.Ordinal)),
            () => Assert.Contains(exact.Reasons, reason => string.Equals(reason, "planned scope affinity", StringComparison.Ordinal)),
            () => Assert.Contains(exact.Reasons, reason => string.Equals(reason, "backend candidate penalty for frontend intent", StringComparison.Ordinal)),
            () => Assert.Contains(
                backend.Candidates.Single(candidate => string.Equals(candidate.Path, "FoodDiary.Web.Client/src/app/coveragebranch.ts", StringComparison.Ordinal)).Reasons,
                reason => string.Equals(reason, "frontend candidate penalty for backend intent", StringComparison.Ordinal)),
            () => Assert.Single(scoped.Candidates, candidate => string.Equals(candidate.Path, "Shared/duplicate-coveragebranch.cs", StringComparison.Ordinal)),
            () => Assert.Contains(scoped.Candidates, candidate => string.Equals(candidate.RecordType, "agent-guide", StringComparison.Ordinal)),
            () => Assert.Contains(scoped.Candidates, candidate =>
                string.Equals(candidate.Path, "FoodDiary.Application.Abstractions/ICoverageBranch.cs", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task SearchAsync_PreservesCoverageForEveryPlannedScope() {
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "scopediversity",
            limit: 2,
            changeType: "Frontend",
            module: null,
            scopePaths: [
                "FoodDiary.Web.Client/src/app/scope-a",
                "FoodDiary.Web.Client/src/app/scope-b",
            ],
            CancellationToken.None,
            expectedChangeSetFingerprint: "fixture-change-set");

        Assert.Multiple(
            () => Assert.Equal(2, result.Candidates.Count),
            () => Assert.Contains(result.Candidates, candidate =>
                candidate.Path.StartsWith("FoodDiary.Web.Client/src/app/scope-a/", StringComparison.Ordinal)),
            () => Assert.Contains(result.Candidates, candidate =>
                candidate.Path.StartsWith("FoodDiary.Web.Client/src/app/scope-b/", StringComparison.Ordinal)),
            () => Assert.All(result.Candidates, candidate =>
                Assert.Contains("planned scope affinity", candidate.Reasons, StringComparer.Ordinal)));
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

    [Fact]
    public async Task SearchAsync_ReturnsUnavailableWhenDatabaseIsCorrupt() {
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
        await File.WriteAllTextAsync(_databasePath, "this is not a SQLite database");
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "anything",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None);

        Assert.False(result.Ready);
        Assert.StartsWith("sqlite-error-", result.UnavailableReason, StringComparison.Ordinal);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task SearchAsync_DoesNotClassifyLockedDatabaseAsCorrupt() {
        await using SqliteConnection blocker = new($"Data Source={_databasePath}");
        await blocker.OpenAsync();
        await using SqliteCommand command = blocker.CreateCommand();
        command.CommandText = "BEGIN EXCLUSIVE; UPDATE metadata SET value = value WHERE key = 'fixture-head';";
        await command.ExecuteNonQueryAsync();
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        WikiContextSearchResult result = await search.SearchAsync(
            "anything",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            CancellationToken.None);

        Assert.False(result.Ready);
        Assert.Equal("sqlite-error-5", result.UnavailableReason);
        Assert.True(File.Exists(_databasePath));
    }

    [Fact]
    public async Task SearchAsync_PropagatesCancellation() {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        SqliteWikiContextSearch search = new(_fixtureRoot, new WikiRuntimeTelemetry());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => search.SearchAsync(
            "anything",
            limit: 10,
            changeType: "Any",
            module: null,
            scopePaths: null,
            cancellation.Token));
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
                ('code', 'logs', 'Modules/Fasting/Presentation/Features/Logs/LogsController.cs', 'logs', 'csharp', 'LogsController', 'web API telemetry logs'),
                ('code', 'privacy', 'FoodDiary.Web.Api/Extensions/TelemetryPrivacyProcessor.cs', 'privacy', 'csharp', 'TelemetryPrivacyProcessor', 'Sanitize SensitiveTags privacy'),
                ('code', 'usda-query', 'FoodDiary.Application.Usda/Queries/SearchUsdaFoods/SearchUsdaFoodsQueryHandler.cs', 'usda-query', 'csharp', 'SearchUsdaFoodsQueryHandler', 'USDA foods search'),
                ('code', 'usda-provider', 'FoodDiary.Integrations/Services/UsdaFoodSearchService.cs', 'usda-provider', 'csharp', 'UsdaFoodSearchService', 'HttpClient external provider'),
                ('code', 'cleanup-options', 'FoodDiary.JobManager/Services/UserLoginEventCleanupOptions.cs', 'cleanup-options', 'csharp', 'UserLoginEventCleanupOptions', 'delete expired user login events schedule'),
                ('code', 'cleanup-job', 'FoodDiary.JobManager/Services/UserLoginEventCleanupJob.cs', 'cleanup-job', 'csharp', 'UserLoginEventCleanupJob', 'delete expired user login events schedule'),
                ('code', 'product-reader-test', 'tests/FoodDiary.Application.Tests/FavoriteMeals/FavoriteMealReadServiceCoverageTests.cs', 'product-reader-test', 'csharp', 'FavoriteMealReadServiceCoverageTests ProductOverviewReadService', 'database reader returns product overview information'),
                ('code', 'product-reader', 'FoodDiary.Infrastructure/Persistence/Products/ProductOverviewReadService.cs', 'product-reader', 'csharp', 'ProductOverviewReadService', 'database reader returns product overview information'),
                ('code', 'wiki-sensitive-data-tool', '.llm-wiki/tools/Find-LlmWikiSensitiveData.ps1', 'wiki-sensitive-data-tool', 'powershell', 'Find Llm Wiki Sensitive Data', 'PowerShell tool finds sensitive domain data touched by a change'),
                ('code', 'wiki-code-graph', '.llm-wiki/tools/code-graph.mjs', 'wiki-code-graph', 'javascript', 'code graph', 'PowerShell tool finds sensitive domain data touched by a change'),
                ('code', 'openai-food-client', 'FoodDiary.Integrations/Services/OpenAi/OpenAiFoodClient.cs', 'openai-food-client', 'csharp', 'OpenAiFoodClient', 'HTTP client sends food recognition requests to OpenAI'),
                ('code', 'paddle-api-client', 'FoodDiary.Integrations/Billing/PaddleApiClient.cs', 'paddle-api-client', 'csharp', 'PaddleApiClient', 'HTTP client sends food recognition requests to OpenAI'),
                ('code', 'integration-uri-validator', 'FoodDiary.Integrations/Options/IntegrationUriValidator.cs', 'integration-uri-validator', 'csharp', 'IntegrationUriValidator', 'validate configured integration URLs before startup'),
                ('code', 'google-token-validator', 'FoodDiary.Integrations/Authentication/GoogleTokenValidator.cs', 'google-token-validator', 'csharp', 'GoogleTokenValidator', 'validate configured integration URLs before startup'),
                ('code', 'pdf-primary', 'Modules/Export/Infrastructure/Services/DiaryPdf/DiaryPdfGenerator.cs', 'pdf-primary', 'csharp', 'DiaryPdfGenerator', 'render diary PDF document generator'),
                ('code', 'pdf-helper', 'Modules/Export/Infrastructure/Services/DiaryPdf/DiaryPdfGenerator.ChartSvgRenderer.cs', 'pdf-helper', 'csharp', 'DiaryPdfGenerator ChartSvgRenderer', 'render diary PDF document generator'),
                ('code', 'cycle-consent-tests', 'tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.ConsentAndConfirmation.cs', 'cycle-consent-tests', 'csharp', 'CyclesFeatureTests ConsentAndConfirmation', 'tests confirm period start update cycle consent owner missing profile invalid user validator failures'),
                ('code', 'cycle-command-validator', 'FoodDiary.Application.Cycles/Commands/ConfirmPeriodStart/ConfirmPeriodStartCommandValidator.cs', 'cycle-command-validator', 'csharp', 'ConfirmPeriodStartCommandValidator', 'confirm period start update cycle consent missing profile invalid user validator failures'),
                ('code', 'authentication-validators', 'tests/FoodDiary.Application.Tests/Authentication/AuthenticationValidatorsTests.cs', 'authentication-validators', 'csharp', 'AuthenticationValidatorsTests', 'tests confirm start missing invalid user validator failures'),
                ('code', 'admin-lesson-parser', 'Modules/Admin/Application/Common/AdminLessonValueParser.cs', 'admin-lesson-parser', 'csharp', 'AdminLessonValueParser', 'parser category difficulty enum field lesson'),
                ('code', 'admin-lesson-validator', 'Modules/Admin/Application/Commands/CreateAdminLesson/CreateAdminLessonCommandValidator.cs', 'admin-lesson-validator', 'csharp', 'CreateAdminLessonCommandValidator', 'validator command lesson category difficulty enum field'),
                ('code', 'generic-enum-parser', 'Modules/Fasting/Application/Common/EnumValueParser.cs', 'generic-enum-parser', 'csharp', 'EnumValueParser', 'parser category difficulty enum field'),
                ('code', 'coverage-exact', 'FoodDiary.Infrastructure/Services/CoverageBranch.cs', 'coverage-exact', 'csharp', 'coveragebranch', 'coveragebranch'),
                ('code', 'coverage-frontend', 'FoodDiary.Web.Client/src/app/coveragebranch.ts', 'coverage-frontend', 'typescript', 'CoverageBranch', 'coveragebranch'),
                ('code', 'scope-a-one', 'FoodDiary.Web.Client/src/app/scope-a/first.ts', 'scope-a-one', 'typescript', 'Scope Diversity First', 'scopediversity'),
                ('code', 'scope-a-two', 'FoodDiary.Web.Client/src/app/scope-a/second.ts', 'scope-a-two', 'typescript', 'Scope Diversity Second', 'scopediversity'),
                ('code', 'scope-b-one', 'FoodDiary.Web.Client/src/app/scope-b/only.ts', 'scope-b-one', 'typescript', 'Scope Diversity Only', 'scopediversity'),
                ('code', 'coverage-abstraction', 'FoodDiary.Application.Abstractions/ICoverageBranch.cs', 'coverage-abstraction', 'csharp', 'ICoverageBranch', 'coveragebranch'),
                ('agent-guide', 'coverage-guide', 'FoodDiary.Infrastructure/AGENTS.md', 'coverage-guide', 'markdown', 'Coverage Branch Guide', 'coveragebranch'),
                ('code', 'program-host-one', 'HostOne/Program.cs', 'program-host-one', 'csharp', 'Program', 'program host startup'),
                ('code', 'program-host-two', 'HostTwo/Program.cs', 'program-host-two', 'csharp', 'Program', 'program host startup'),
                ('code', 'coverage-duplicate-one', 'Shared/duplicate-coveragebranch.cs', 'coverage-duplicate-one', 'csharp', 'DuplicateCoverageBranch', 'coveragebranch'),
                ('code', 'coverage-duplicate-two', 'Shared/duplicate-coveragebranch.cs', 'coverage-duplicate-two', 'csharp', 'DuplicateCoverageBranch', 'coveragebranch');
            CREATE VIRTUAL TABLE context_search_identity USING fts5(path, title, tokenize = 'unicode61 remove_diacritics 2');
            INSERT INTO context_search_identity(rowid, path, title) SELECT rowid, path, title FROM context_search;
            """;
        command.ExecuteNonQuery();
    }
}
