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
            "FoodDiary.Infrastructure/Services/DiaryPdf/DiaryPdfGenerator.cs",
            result.Candidates[0].Path);
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
    }

    [Fact]
    public async Task SearchAsync_PrefersBehaviorSpecificPartialTestForExplicitTestIntent() {
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
            "tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.ConsentAndConfirmation.cs",
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
                "FoodDiary.Application.Admin/Common/AdminLessonValueParser.cs",
                StringComparison.Ordinal));
        WikiContextSearchCandidate validator = Assert.Single(
            result.Candidates,
            candidate => string.Equals(
                candidate.Path,
                "FoodDiary.Application.Admin/Commands/CreateAdminLesson/CreateAdminLessonCommandValidator.cs",
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
                ('code', 'logs', 'FoodDiary.Presentation.Api/Features/Logs/LogsController.cs', 'logs', 'csharp', 'LogsController', 'web API telemetry logs'),
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
                ('code', 'pdf-primary', 'FoodDiary.Infrastructure/Services/DiaryPdf/DiaryPdfGenerator.cs', 'pdf-primary', 'csharp', 'DiaryPdfGenerator', 'render diary PDF document generator'),
                ('code', 'pdf-helper', 'FoodDiary.Infrastructure/Services/DiaryPdf/DiaryPdfGenerator.ChartSvgRenderer.cs', 'pdf-helper', 'csharp', 'DiaryPdfGenerator ChartSvgRenderer', 'render diary PDF document generator'),
                ('code', 'cycle-consent-tests', 'tests/FoodDiary.Application.Tests/Cycles/CyclesFeatureTests.ConsentAndConfirmation.cs', 'cycle-consent-tests', 'csharp', 'CyclesFeatureTests ConsentAndConfirmation', 'tests confirm period start update cycle consent owner missing profile invalid user validator failures'),
                ('code', 'cycle-command-validator', 'FoodDiary.Application.Cycles/Commands/ConfirmPeriodStart/ConfirmPeriodStartCommandValidator.cs', 'cycle-command-validator', 'csharp', 'ConfirmPeriodStartCommandValidator', 'confirm period start update cycle consent missing profile invalid user validator failures'),
                ('code', 'authentication-validators', 'tests/FoodDiary.Application.Tests/Authentication/AuthenticationValidatorsTests.cs', 'authentication-validators', 'csharp', 'AuthenticationValidatorsTests', 'tests confirm start missing invalid user validator failures'),
                ('code', 'admin-lesson-parser', 'FoodDiary.Application.Admin/Common/AdminLessonValueParser.cs', 'admin-lesson-parser', 'csharp', 'AdminLessonValueParser', 'parser category difficulty enum field lesson'),
                ('code', 'admin-lesson-validator', 'FoodDiary.Application.Admin/Commands/CreateAdminLesson/CreateAdminLessonCommandValidator.cs', 'admin-lesson-validator', 'csharp', 'CreateAdminLessonCommandValidator', 'validator command lesson category difficulty enum field'),
                ('code', 'generic-enum-parser', 'FoodDiary.Application.Fasting/Common/EnumValueParser.cs', 'generic-enum-parser', 'csharp', 'EnumValueParser', 'parser category difficulty enum field'),
                ('code', 'coverage-exact', 'FoodDiary.Infrastructure/Services/CoverageBranch.cs', 'coverage-exact', 'csharp', 'coveragebranch', 'coveragebranch'),
                ('code', 'coverage-frontend', 'FoodDiary.Web.Client/src/app/coveragebranch.ts', 'coverage-frontend', 'typescript', 'CoverageBranch', 'coveragebranch'),
                ('code', 'coverage-abstraction', 'FoodDiary.Application.Abstractions/ICoverageBranch.cs', 'coverage-abstraction', 'csharp', 'ICoverageBranch', 'coveragebranch'),
                ('agent-guide', 'coverage-guide', 'FoodDiary.Infrastructure/AGENTS.md', 'coverage-guide', 'markdown', 'Coverage Branch Guide', 'coveragebranch'),
                ('code', 'coverage-duplicate-one', 'Shared/duplicate-coveragebranch.cs', 'coverage-duplicate-one', 'csharp', 'DuplicateCoverageBranch', 'coveragebranch'),
                ('code', 'coverage-duplicate-two', 'Shared/duplicate-coveragebranch.cs', 'coverage-duplicate-two', 'csharp', 'DuplicateCoverageBranch', 'coveragebranch');
            """;
        command.ExecuteNonQuery();
    }
}
