using NSubstitute;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class WikiQueryServiceTests {
    private readonly IWikiCommandExecutor _executor = Substitute.For<IWikiCommandExecutor>();
    private readonly IChangeSetSnapshotService _snapshots = Substitute.For<IChangeSetSnapshotService>();

    public WikiQueryServiceTests() {
        ChangeSetSnapshot snapshot = new(
            "abc123",
            "snapshot-hash",
            ["FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs"],
            DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
    }

    [Fact]
    public async Task GetChangeContextAsync_PassesIntentAndPlannedPathAsSeparateArguments() {
        WikiQueryService service = new(_executor, _snapshots);
        CancellationToken cancellationToken = new();

        await service.GetChangeContextAsync(
            "Add MCP; Write-Error must stay data",
            "FoodDiary.Development.Mcp",
            compact: true,
            cancellationToken);

        await _executor.Received(1).ExecuteAsync(
            "brief",
            Arg.Is<IReadOnlyList<string>>(arguments =>
                arguments.SequenceEqual(new[] {
                    "-Format",
                    "Json",
                    "-Objective",
                    "Add MCP; Write-Error must stay data",
                    "-Compact",
                    "-ProposedPath",
                    "FoodDiary.Development.Mcp",
                    "-ChangedPath",
                    "FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs",
                })),
            cancellationToken);
    }

    [Fact]
    public async Task TraceBackendFlowAsync_RejectsBlankQuery() {
        WikiQueryService service = new(_executor, _snapshots);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.TraceBackendFlowAsync(" ", CancellationToken.None));
    }

    [Fact]
    public async Task GetTestPlanAsync_UsesCurrentChangeSetWhenQueryIsMissing() {
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetTestPlanAsync(
            intent: null,
            plannedPaths: null,
            changedPaths: null,
            executedChecks: null,
            CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.SequenceEqual(new[] {
                "-Format",
                "Json",
                "-BaseRef",
                "abc123",
                "-ChangedPath",
                "FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs",
            })),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetTestPlanAsync_PreservesPlannedPathsAlongsideCurrentChanges() {
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetTestPlanAsync(
            intent: "Change measurement presentation",
            plannedPaths: ["FoodDiary.Web.Client/src/app/features/weight-history"],
            changedPaths: null,
            executedChecks: null,
            CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.SequenceEqual(new[] {
                "-Format",
                "Json",
                "-Objective",
                "Change measurement presentation",
                "-NoBaseline",
                "-ProposedPath",
                "FoodDiary.Web.Client/src/app/features/weight-history",
            })),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetTestPlanAsync_ForwardsExecutedChecksAsRequestScopedEvidence() {
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetTestPlanAsync(
            intent: null,
            plannedPaths: null,
            changedPaths: ["FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs"],
            executedChecks: ["dotnet test tests/FoodDiary.Development.Mcp.Tests/FoodDiary.Development.Mcp.Tests.csproj"],
            cancellationToken: CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.Contains("-ExecutedCheck", StringComparer.Ordinal) &&
                arguments.Contains("dotnet test tests/FoodDiary.Development.Mcp.Tests/FoodDiary.Development.Mcp.Tests.csproj", StringComparer.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_RunsAllQueriesAgainstOneSnapshot() {
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Change a backend flow",
            "SomeCommand",
            "FoodDiary.Application.Users",
            CancellationToken.None);

        Assert.Equal("snapshot-hash", result.SnapshotFingerprint);
        await _snapshots.Received(2).GetAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            CancellationToken.None);
        await _snapshots.Received(1).RefreshAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            CancellationToken.None);
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.SequenceEqual(new[] {
                "-Format",
                "Json",
                "-Fast",
                "-Objective",
                "Change a backend flow",
                "-NoBaseline",
                "-ProposedPath",
                "FoodDiary.Application.Users",
            })),
            CancellationToken.None);
        await _executor.Received(1).ExecuteAsync(
            "brief",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.Contains(
                "-SkipTestPlan",
                StringComparer.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_UsesFreshSqlContextAsPrimaryScope() {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult sqlContext = new(
            "sqlite-derived",
            "in-process-microsoft-data-sqlite",
            Ready: true,
            IndexedDocuments: 20_000,
            Fingerprint: "fts-fingerprint",
            UpdatedAtUtc: "2026-08-21T00:00:00Z",
            ChangeSetFingerprint: "snapshot-hash",
            GitHead: "abc123",
            Fresh: true,
            QueryTerms: ["user", "privacy"],
            Candidates: [new WikiContextSearchCandidate(
                1,
                "FoodDiary.Web.Api/Extensions/TelemetryPrivacyProcessor.cs",
                "code",
                "csharp",
                100,
                -1,
                ["ranking policy web-api-intent"])],
            QueryDurationMilliseconds: 12.5);
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(sqlContext);
        WikiRuntimeTelemetry telemetry = new();
        WikiQueryService service = new(
            _executor,
            _snapshots,
            queryCache: null,
            contextSearch,
            telemetry);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Protect telemetry",
            "strip user identity from web API telemetry logs",
            "FoodDiary.Web.Api/Extensions",
            CancellationToken.None);

        Assert.Same(sqlContext, result.SqlContextSearch);
        Assert.Equal("sqlite", result.ContextRetrievalSource);
        Assert.Null(result.ContextFallbackReason);
        Assert.Contains(
            sqlContext.Candidates[0].Path,
            result.ExpandedScopePaths,
            StringComparer.OrdinalIgnoreCase);
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
        await contextSearch.Received(1).SearchAsync(
            query: "strip user identity from web API telemetry logs",
            limit: 20,
            changeType: "Api",
            module: null,
            scopePaths: Arg.Is<IReadOnlyList<string>>(paths => paths.SequenceEqual(
                new[] { "FoodDiary.Web.Api/Extensions" },
                StringComparer.OrdinalIgnoreCase)),
            cancellationToken: CancellationToken.None,
            expectedChangeSetFingerprint: "snapshot-hash");
        WikiCommandStageTiming timing = Assert.Single(
            telemetry.Capture(0).CommandStageTimings,
            item => string.Equals(item.Command, "context-routing", StringComparison.Ordinal));
        Assert.Equal("sqlite-primary", timing.Stage);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_RebuildsStaleSqlIndexBeforeUsingIt() {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult stale = CreateSqlContext(
            ready: false,
            fresh: false,
            unavailableReason: "snapshot-mismatch");
        WikiContextSearchResult fresh = CreateSqlContext(ready: true, fresh: true);
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(stale, fresh);
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Protect telemetry",
            "strip user identity from web API telemetry logs",
            "FoodDiary.Web.Api/Extensions",
            CancellationToken.None);

        Assert.Equal("sqlite", result.ContextRetrievalSource);
        Assert.Same(fresh, result.SqlContextSearch);
        await _executor.Received(1).ExecuteAsync(
            "graph-build",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.SequenceEqual(new[] { "-Format", "Json" })),
            CancellationToken.None);
        await contextSearch.Received(2).SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            "snapshot-hash");
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("sqlite-error-11")]
    [InlineData("sqlite-error-26")]
    public async Task GetDevelopmentContextAsync_RebuildsConfirmedCorruptSqlIndexBeforeUsingIt(
        string unavailableReason) {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult corrupt = CreateSqlContext(
            ready: false,
            fresh: false,
            unavailableReason);
        WikiContextSearchResult fresh = CreateSqlContext(ready: true, fresh: true);
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(corrupt, fresh);
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Change user flow",
            "update user",
            "FoodDiary.Application.Users",
            CancellationToken.None);

        Assert.Equal("sqlite", result.ContextRetrievalSource);
        Assert.Same(fresh, result.SqlContextSearch);
        await _executor.Received(1).ExecuteAsync(
            "graph-build",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_ReportsLockedSqlIndexWithoutAutomaticTrace() {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult locked = CreateSqlContext(
            ready: false,
            fresh: false,
            unavailableReason: "sqlite-error-5");
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(locked);
        WikiRuntimeTelemetry telemetry = new();
        WikiQueryService service = new(
            _executor,
            _snapshots,
            contextSearch: contextSearch,
            telemetry: telemetry);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Change user flow",
            "update user",
            "FoodDiary.Application.Users",
            CancellationToken.None);

        Assert.Equal("unavailable", result.ContextRetrievalSource);
        Assert.Equal("sqlite-error-5", result.ContextFallbackReason);
        Assert.Null(result.BackendTrace);
        Assert.True(result.PartialSuccess);
        Assert.Contains(result.ComponentErrors, error =>
            string.Equals(error.ErrorCode, DevelopmentMcpErrorCodes.ContextSearchUnavailable, StringComparison.Ordinal) &&
            error.Message.Contains("locked", StringComparison.OrdinalIgnoreCase));
        WikiCommandStageTiming route = Assert.Single(
            telemetry.Capture(0).CommandStageTimings,
            item => string.Equals(item.Command, "context-routing", StringComparison.Ordinal));
        Assert.Equal("sqlite-unavailable", route.Stage);
        await _executor.DidNotReceive().ExecuteAsync(
            "graph-build",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_ReportsRecoveryWhenRebuiltIndexIsStillStale() {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult stale = CreateSqlContext(
            ready: false,
            fresh: false,
            unavailableReason: "snapshot-mismatch");
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(stale);
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Change user flow",
            "update user",
            "FoodDiary.Application.Users",
            CancellationToken.None);

        Assert.Equal("unavailable", result.ContextRetrievalSource);
        Assert.Equal("snapshot-mismatch", result.ContextFallbackReason);
        Assert.Equal(["FoodDiary.Application.Users"], result.ExpandedScopePaths);
        Assert.Contains(result.ComponentErrors, error =>
            string.Equals(error.ErrorCode, DevelopmentMcpErrorCodes.ContextSearchUnavailable, StringComparison.Ordinal) &&
            error.Message.Contains("worktree", StringComparison.OrdinalIgnoreCase));
        await _executor.Received(1).ExecuteAsync(
            "graph-build",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_RejectsSqlResultWhenAnyWorktreePathChanges() {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult fresh = CreateSqlContext(ready: true, fresh: true);
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(fresh);
        ChangeSetSnapshot initial = new(
            "abc123",
            "snapshot-hash",
            ["FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs"],
            DateTimeOffset.UtcNow);
        ChangeSetSnapshot changed = initial with {
            Fingerprint = "changed-outside-selected-scope",
            ChangedPaths = ["docs/unrelated.md"],
        };
        _snapshots.RefreshAsync(
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>()).Returns(initial, changed);
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        DevelopmentMcpException exception = await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
            service.GetDevelopmentContextAsync(
                "Protect telemetry",
                "strip user identity from web API telemetry logs",
                "FoodDiary.Web.Api/Extensions",
                CancellationToken.None));

        Assert.Equal(DevelopmentMcpErrorCodes.SnapshotChanged, exception.ErrorCode);
        Assert.Contains("complete worktree", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_RejectsResultWhenSnapshotChangesDuringComponents() {
        _snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(new ChangeSetSnapshot(
            "abc123",
            "changed-snapshot",
            ["FoodDiary.Development.Mcp/Wiki/WikiTools.cs"],
            DateTimeOffset.UtcNow));
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentMcpException exception = await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
            service.GetDevelopmentContextAsync(
                "Change backend flow",
                "SomeCommand",
                "FoodDiary.Application.Users",
                CancellationToken.None));

        Assert.Equal(DevelopmentMcpErrorCodes.SnapshotChanged, exception.ErrorCode);
        await _executor.Received(1).ExecuteAsync(
            "brief",
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_UsesPlannedPathForCleanWorktreeTestPlan() {
        ChangeSetSnapshot snapshot = new(
            "abc123",
            "clean-snapshot",
            [],
            DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetDevelopmentContextAsync(
            "Change frontend measurements",
            "MeasurementSystem",
            "FoodDiary.Web.Client/src/app",
            CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.Contains(
                "FoodDiary.Web.Client/src/app",
                StringComparer.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_ReportsExplicitRecoveryWhenSqlReaderIsNotConfigured() {
        ChangeSetSnapshot snapshot = new(
            "abc123",
            "clean-snapshot",
            [],
            DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Add cycle prediction",
            "cycle prediction",
            plannedPath: null,
            CancellationToken.None);

        Assert.True(result.PartialSuccess);
        Assert.Empty(result.ExpandedScopePaths);
        Assert.Equal("unavailable", result.ContextRetrievalSource);
        Assert.Contains(result.ComponentErrors, error =>
            string.Equals(error.ErrorCode, DevelopmentMcpErrorCodes.ContextSearchUnavailable, StringComparison.Ordinal) &&
            error.Message.Contains("graph-build", StringComparison.Ordinal));
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_ReturnsPartialResultWhenTestPlanFails() {
        _executor.ExecuteAsync("test-plan", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<Task<WikiCommandResult>>(_ => throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.WikiCommandFailed,
                "test plan failed"));
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Change backend flow",
            "SomeCommand",
            "FoodDiary.Application.Users",
            CancellationToken.None);

        Assert.True(result.PartialSuccess);
        Assert.Null(result.TestPlan);
        Assert.Contains(result.ComponentErrors, error => string.Equals(
            error.Component,
            "test-plan",
            StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_UsesSqlCandidatesToExpandNarrowPlannedPath() {
        ChangeSetSnapshot snapshot = new(
            "abc123",
            "clean-snapshot",
            [],
            DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult sqlContext = CreateSqlContext(ready: true, fresh: true) with {
            Candidates = [new WikiContextSearchCandidate(
                1,
                "FoodDiary.Application.WeeklyCheckIn/CyclePredictionService.cs",
                "code",
                "csharp",
                100,
                -1,
                ["SQLite FTS5 lexical match"])],
        };
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(sqlContext);
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Add cycle prediction across frontend and backend",
            "cycle prediction",
            "FoodDiary.Web.Client/src/app/features/cycle-tracking",
            CancellationToken.None);

        Assert.False(result.ScopeMismatch);
        Assert.True(result.CrossLayerScope);
        Assert.Equal(["Application", "Frontend"], result.EffectiveLayers);
        await _executor.Received(1).ExecuteAsync(
            "brief",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.Contains(
                "FoodDiary.Application.WeeklyCheckIn/CyclePredictionService.cs",
                StringComparer.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetTestPlanAsync_PrefersExplicitChangesAndKeepsPlannedScope() {
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetTestPlanAsync(
            "Change frontend measurements",
            ["planned/path"],
            ["explicit/changed.cs"],
            executedChecks: null,
            cancellationToken: CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments =>
                arguments.Contains("explicit/changed.cs", StringComparer.Ordinal) &&
                arguments.Contains("planned/path", StringComparer.Ordinal) &&
                !arguments.Contains("FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs", StringComparer.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetTestPlanAsync_RequiresScopeForCleanWorktree() {
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(new ChangeSetSnapshot(
            "abc123",
            "clean-snapshot",
            [],
            DateTimeOffset.UtcNow));
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentMcpException exception = await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
            service.GetTestPlanAsync(
                intent: null,
                plannedPaths: null,
                changedPaths: null,
                executedChecks: null,
                cancellationToken: CancellationToken.None));

        Assert.Equal(DevelopmentMcpErrorCodes.TestPlanScopeRequired, exception.ErrorCode);
    }

    [Fact]
    public async Task GetTestPlanAsync_ReportsUnavailableBaselineForCleanPlannedScope() {
        ChangeSetSnapshot snapshot = new("abc123", "clean-snapshot", [], DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetTestPlanAsync(
            intent: "Inspect MCP",
            plannedPaths: ["FoodDiary.Development.Mcp"],
            changedPaths: null,
            executedChecks: null,
            cancellationToken: CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments =>
                arguments.Contains("-NoBaseline", StringComparer.Ordinal) &&
                !arguments.Contains("-BaseRef", StringComparer.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_KeepsPlannedScopeWhenSqlIsUnavailable() {
        ChangeSetSnapshot snapshot = new("abc123", "clean-snapshot", [], DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Update user",
            "UpdateUser",
            "FoodDiary.Application.Users",
            CancellationToken.None);

        Assert.False(result.ScopeMismatch);
        Assert.Equal(["FoodDiary.Application.Users"], result.ExpandedScopePaths);
        Assert.True(result.PartialSuccess);
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
        Assert.False(result.BaselineAvailable);
    }

    [Fact]
    public async Task GetChangeContextAsync_ReusesSuccessfulResultForUnchangedSnapshot() {
        WikiRuntimeTelemetry telemetry = new();
        WikiQueryCache cache = new(TimeProvider.System, telemetry);
        WikiQueryService service = new(_executor, _snapshots, cache);
        _executor.ExecuteAsync("brief", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(CreateResult("brief", []));

        WikiCommandResult first = await service.GetChangeContextAsync(
            "Improve MCP cache",
            "FoodDiary.Development.Mcp",
            compact: true,
            CancellationToken.None);
        WikiCommandResult second = await service.GetChangeContextAsync(
            "Improve MCP cache",
            "FoodDiary.Development.Mcp",
            compact: true,
            CancellationToken.None);

        Assert.Same(first, second);
        await _executor.Received(1).ExecuteAsync(
            "brief",
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
        WikiRuntimeMetrics metrics = cache.CaptureMetrics();
        Assert.Equal(1, metrics.QueryCache.Hits);
        Assert.Equal(1, metrics.QueryCache.Misses);
        Assert.Equal(0.5, metrics.QueryCache.HitRate);
    }

    [Fact]
    public async Task GetChangeContextAsync_RequestsOnlyThePlannedSnapshotScope() {
        ChangeSetSnapshot snapshot = new(
            "abc123",
            "scoped-snapshot",
            ["FoodDiary.Web.Client/src/app/unrelated.ts"],
            DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(snapshot);
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetChangeContextAsync(
            "Inspect mediator behavior",
            "Shared/FoodDiary.Mediator",
            compact: true,
            CancellationToken.None);

        await _snapshots.Received(1).GetAsync(
            Arg.Is<IReadOnlyList<string>>(paths => paths.SequenceEqual(
                new[] { "Shared/FoodDiary.Mediator" },
                StringComparer.OrdinalIgnoreCase)),
            CancellationToken.None);
        await _executor.Received(1).ExecuteAsync(
            "brief",
            Arg.Is<IReadOnlyList<string>>(arguments =>
                !arguments.Contains("-ChangedPath", StringComparer.Ordinal) &&
                arguments.Contains("Shared/FoodDiary.Mediator", StringComparer.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetChangeContextAsync_InvalidatesCacheWhenSnapshotFingerprintChanges() {
        ChangeSetSnapshot initial = new("abc123", "first", ["one.cs"], DateTimeOffset.UtcNow);
        ChangeSetSnapshot changed = new("abc123", "second", ["one.cs"], DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(initial, changed);
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetChangeContextAsync("Improve MCP", plannedPath: null, compact: true, CancellationToken.None);
        await service.GetChangeContextAsync("Improve MCP", plannedPath: null, compact: true, CancellationToken.None);

        await _executor.Received(2).ExecuteAsync(
            "brief",
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TraceBackendFlowAsync_ReturnsExecutorResultForValidQuery() {
        ChangeSetSnapshot snapshot = new("abc123", "trace-snapshot", [], DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        WikiCommandResult expected = CreateResult("trace", ["FoodDiary.Application.Users/Handler.cs"]);
        _executor.ExecuteAsync("trace", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(expected);
        WikiQueryService service = new(_executor, _snapshots);

        WikiCommandResult result = await service.TraceBackendFlowAsync("SomeQuery", CancellationToken.None);

        Assert.Same(expected, result);
        await _executor.Received(1).ExecuteAsync(
            "trace",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.SequenceEqual(new[] {
                "-Format",
                "Json",
                "-Fast",
                "-Query",
                "SomeQuery",
            })),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetTestPlanAsync_RejectsIdenticalBaseAndHeadRevisions() {
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentMcpException exception = await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
            service.GetTestPlanAsync(
                intent: null,
                plannedPaths: ["FoodDiary.Development.Mcp"],
                changedPaths: null,
                executedChecks: null,
                baseRevision: "REV123",
                headRevision: "rev123",
                cancellationToken: CancellationToken.None));

        Assert.Equal(DevelopmentMcpErrorCodes.InvalidRevisionRange, exception.ErrorCode);
    }

    [Fact]
    public async Task GetTestPlanAsync_UsesExplicitBaseAndHeadRevisionsInsteadOfSnapshotHead() {
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetTestPlanAsync(
            intent: null,
            plannedPaths: null,
            changedPaths: null,
            executedChecks: null,
            baseRevision: "explicit-base",
            headRevision: "explicit-head",
            cancellationToken: CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments =>
                arguments.Contains("-BaseRef", StringComparer.Ordinal) &&
                arguments.Contains("explicit-base", StringComparer.Ordinal) &&
                arguments.Contains("-HeadRef", StringComparer.Ordinal) &&
                arguments.Contains("explicit-head", StringComparer.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetTestPlanAsync_AddsHeadRefEvenWithoutBaseline() {
        ChangeSetSnapshot cleanSnapshot = new("abc123", "clean-snapshot", [], DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(cleanSnapshot);
        WikiQueryService service = new(_executor, _snapshots);

        await service.GetTestPlanAsync(
            intent: null,
            plannedPaths: ["FoodDiary.Development.Mcp"],
            changedPaths: null,
            executedChecks: null,
            baseRevision: null,
            headRevision: "explicit-head-only",
            cancellationToken: CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments =>
                arguments.Contains("-NoBaseline", StringComparer.Ordinal) &&
                !arguments.Contains("-BaseRef", StringComparer.Ordinal) &&
                arguments.Contains("-HeadRef", StringComparer.Ordinal) &&
                arguments.Contains("explicit-head-only", StringComparer.Ordinal)),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_InfersInfrastructureDomainAndApiLayersAndIgnoresUnknownPaths() {
        ChangeSetSnapshot snapshot = new("abc123", "clean-snapshot", [], DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        string[] paths = [
            "FoodDiary.Infrastructure/Persistence/UserRepository.cs",
            "FoodDiary.Domain/Users/User.cs",
            "FoodDiary.Web.Api/Controllers/UsersController.cs",
            "docs/unrelated-notes.md",
        ];
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult sqlContext = CreateSqlContext(ready: true, fresh: true) with {
            Candidates = [.. paths.Select((path, index) => new WikiContextSearchCandidate(
                index + 1,
                path,
                "code",
                "csharp",
                100 - index,
                -1,
                ["SQLite FTS5 lexical match"]))],
        };
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(sqlContext);
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Change user persistence",
            "UpdateUser",
            plannedPath: null,
            CancellationToken.None);

        Assert.Equal(["Api", "Domain", "Infrastructure"], result.EffectiveLayers);
        Assert.True(result.CrossLayerScope);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_ReportsMissingSqlReaderAsComponentError() {
        ChangeSetSnapshot snapshot = new("abc123", "clean-snapshot", [], DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(snapshot);
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Investigate flow",
            "SomeQuery",
            plannedPath: null,
            CancellationToken.None);

        Assert.True(result.PartialSuccess);
        Assert.Null(result.BackendTrace);
        Assert.Contains(
            result.ComponentErrors,
            error => string.Equals(error.Component, "context-search", StringComparison.Ordinal) &&
                string.Equals(
                    error.ErrorCode,
                    DevelopmentMcpErrorCodes.ContextSearchUnavailable,
                    StringComparison.Ordinal));
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_ReportsRecoveryWhenGraphRebuildThrows() {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult stale = CreateSqlContext(
            ready: false,
            fresh: false,
            unavailableReason: "database-missing");
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(stale);
        _executor.ExecuteAsync("graph-build", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<Task<WikiCommandResult>>(_ => throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.WikiUnavailable,
                "graph rebuild failed"));
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Change user flow",
            "update user",
            "FoodDiary.Application.Users",
            CancellationToken.None);

        Assert.Equal("unavailable", result.ContextRetrievalSource);
        Assert.Equal($"graph-refresh-{DevelopmentMcpErrorCodes.WikiUnavailable}", result.ContextFallbackReason);
        Assert.Contains(result.ComponentErrors, error =>
            string.Equals(error.ErrorCode, DevelopmentMcpErrorCodes.ContextSearchUnavailable, StringComparison.Ordinal) &&
            error.Message.Contains("graph-build", StringComparison.Ordinal));
        await _executor.Received(1).ExecuteAsync(
            "graph-build",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_PropagatesCancelledGraphRefreshWithoutAutomaticTrace() {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(CreateSqlContext(
                ready: false,
                fresh: false,
                unavailableReason: "database-missing"));
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        _executor.ExecuteAsync("graph-build", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<WikiCommandResult>(cancellation.Token));
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetDevelopmentContextAsync(
            "Change user flow",
            "update user",
            "FoodDiary.Application.Users",
            cancellation.Token));

        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_TreatsFreshEmptyCandidatesAsNoSqlCandidates() {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult freshButEmpty = new(
            Authority: "sqlite-derived",
            Reader: "in-process-microsoft-data-sqlite",
            Ready: true,
            IndexedDocuments: 20_000,
            Fingerprint: "fts-fingerprint",
            UpdatedAtUtc: "2026-08-21T00:00:00Z",
            ChangeSetFingerprint: "snapshot-hash",
            GitHead: "abc123",
            Fresh: true,
            QueryTerms: ["user"],
            Candidates: [],
            QueryDurationMilliseconds: 1);
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(freshButEmpty);
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Change user flow",
            "update user",
            "FoodDiary.Application.Users",
            CancellationToken.None);

        Assert.Equal("unavailable", result.ContextRetrievalSource);
        Assert.Equal("sqlite-no-candidates", result.ContextFallbackReason);
        Assert.Contains(result.ComponentErrors, error =>
            string.Equals(error.ErrorCode, DevelopmentMcpErrorCodes.ContextSearchUnavailable, StringComparison.Ordinal) &&
            error.Message.Contains("Refine the query", StringComparison.Ordinal));
        await _executor.DidNotReceive().ExecuteAsync(
            "graph-build",
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
        await _executor.DidNotReceive().ExecuteAsync(
            "trace",
            Arg.Any<IReadOnlyList<string>>(),
            CancellationToken.None);
    }

    [Theory]
    [InlineData(null, "Any")]
    [InlineData("FoodDiary.Web.Client/src/app/feature.ts", "Frontend")]
    [InlineData("FoodDiary.Infrastructure/Migrations/2026_AddIndex.cs", "Database")]
    [InlineData("tests/FoodDiary.Application.Tests/SomeTest.cs", "Tests")]
    [InlineData("FoodDiary.Application.Users/Handler.cs", "Backend")]
    public async Task GetDevelopmentContextAsync_InfersSearchChangeTypeFromPlannedPath(
        string? plannedPath,
        string expectedChangeType) {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        WikiContextSearchResult fresh = CreateSqlContext(ready: true, fresh: true);
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(fresh);
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        await service.GetDevelopmentContextAsync(
            "Intent text",
            "search query",
            plannedPath,
            CancellationToken.None);

        await contextSearch.Received(1).SearchAsync(
            query: Arg.Is<string>(value => string.Equals(value, "search query", StringComparison.Ordinal)),
            limit: Arg.Is(20),
            changeType: Arg.Is<string>(value => string.Equals(value, expectedChangeType, StringComparison.Ordinal)),
            module: Arg.Is<string?>(value => ReferenceEquals(value, null)),
            scopePaths: Arg.Is<IReadOnlyList<string>?>(paths => ReferenceEquals(plannedPath, null)
                ? !ReferenceEquals(paths, null) && paths.Count == 0
                : !ReferenceEquals(paths, null) && paths.Count == 1 && string.Equals(paths[0], plannedPath, StringComparison.Ordinal)),
            cancellationToken: Arg.Is(CancellationToken.None),
            expectedChangeSetFingerprint: Arg.Is<string?>(value => !string.IsNullOrWhiteSpace(value)));
    }

    [Theory]
    [InlineData("тест parser значений enum", "Tests")]
    [InlineData("tests for the Web Push sender", "Tests")]
    [InlineData("component specs for auth dialog", "Tests")]
    [InlineData("update user handler", "Any")]
    public async Task GetDevelopmentContextAsync_InfersTestSearchTypeFromQueryWithoutPlannedPath(
        string query,
        string expectedChangeType) {
        IWikiContextSearch contextSearch = Substitute.For<IWikiContextSearch>();
        contextSearch.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>()).Returns(CreateSqlContext(ready: true, fresh: true));
        WikiQueryService service = new(_executor, _snapshots, contextSearch: contextSearch);

        await service.GetDevelopmentContextAsync(
            "Intent text",
            query,
            plannedPath: null,
            CancellationToken.None);

        await contextSearch.Received(1).SearchAsync(
            query: Arg.Is<string>(value => string.Equals(value, query, StringComparison.Ordinal)),
            limit: Arg.Is(20),
            changeType: Arg.Is<string>(value => string.Equals(value, expectedChangeType, StringComparison.Ordinal)),
            module: Arg.Is<string?>(value => ReferenceEquals(value, null)),
            scopePaths: Arg.Is<IReadOnlyList<string>?>(paths =>
                !ReferenceEquals(paths, null) && paths.Count == 0),
            cancellationToken: Arg.Is(CancellationToken.None),
            expectedChangeSetFingerprint: Arg.Is<string?>(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static WikiCommandResult CreateResult(
        string command,
        IReadOnlyList<string> referencedPaths,
        IReadOnlyList<string>? scopePaths = null) => new(
        command,
        RawOutput: null,
        StructuredOutput: null,
        RepositoryRoot: "repository",
        GitHead: "abc123",
        OutputLines: [],
        ReferencedPaths: referencedPaths,
        RequiredChecks: [],
        Warnings: [],
        ScopePaths: scopePaths);

    private static WikiContextSearchResult CreateSqlContext(
        bool ready,
        bool fresh,
        string? unavailableReason = null) => new(
        Authority: "sqlite-derived",
        Reader: "in-process-microsoft-data-sqlite",
        Ready: ready,
        IndexedDocuments: 20_000,
        Fingerprint: "fts-fingerprint",
        UpdatedAtUtc: "2026-08-21T00:00:00Z",
        ChangeSetFingerprint: "snapshot-hash",
        GitHead: "abc123",
        Fresh: fresh,
        QueryTerms: ["user"],
        Candidates: ready
            ? [new WikiContextSearchCandidate(
                1,
                "FoodDiary.Application.Users/PrimaryHandler.cs",
                "code",
                "csharp",
                100,
                -1,
                ["SQLite FTS5 lexical match"])]
            : [],
        QueryDurationMilliseconds: 1,
        UnavailableReason: unavailableReason);
}
