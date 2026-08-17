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
        _snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
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
                "-BaseRef",
                "abc123",
                "-ChangedPath",
                "FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs",
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
        await _snapshots.Received(1).GetAsync(CancellationToken.None);
        await _snapshots.Received(2).RefreshAsync(CancellationToken.None);
        await _executor.Received(1).ExecuteAsync(
            "trace",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.SequenceEqual(new[] {
                "-Format",
                "Json",
                "-Fast",
                "-Query",
                "SomeCommand",
            })),
            CancellationToken.None);
        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.SequenceEqual(new[] {
                "-Format",
                "Json",
                "-Fast",
                "-Objective",
                "Change a backend flow",
                "-BaseRef",
                "abc123",
                "-ChangedPath",
                "FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs",
                "-ProposedPath",
                "FoodDiary.Application.Users",
            })),
            CancellationToken.None);
    }

    [Fact]
    public async Task GetDevelopmentContextAsync_RejectsResultWhenSnapshotChangesAfterTrace() {
        _snapshots.RefreshAsync(Arg.Any<CancellationToken>()).Returns(new ChangeSetSnapshot(
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
        await _executor.DidNotReceive().ExecuteAsync(
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
        _snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
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
    public async Task GetDevelopmentContextAsync_UsesTracePathsWhenPlannedPathIsMissing() {
        ChangeSetSnapshot snapshot = new(
            "abc123",
            "clean-snapshot",
            [],
            DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        _executor.ExecuteAsync("trace", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(CreateResult("trace", ["FoodDiary.Application.WeeklyCheckIn/CyclePredictionService.cs"]));
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Add cycle prediction",
            "cycle prediction",
            plannedPath: null,
            CancellationToken.None);

        Assert.False(result.PartialSuccess);
        Assert.Contains(
            "FoodDiary.Application.WeeklyCheckIn/CyclePredictionService.cs",
            result.ExpandedScopePaths,
            StringComparer.Ordinal);
        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.Contains(
                "FoodDiary.Application.WeeklyCheckIn/CyclePredictionService.cs",
                StringComparer.Ordinal)),
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
    public async Task GetDevelopmentContextAsync_FlagsScopeMismatchAndExpandsNarrowPlannedPath() {
        ChangeSetSnapshot snapshot = new(
            "abc123",
            "clean-snapshot",
            [],
            DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        _executor.ExecuteAsync("trace", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(CreateResult("trace", ["FoodDiary.Application.WeeklyCheckIn/CyclePredictionService.cs"]));
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Add cycle prediction across frontend and backend",
            "cycle prediction",
            "FoodDiary.Web.Client/src/app/features/cycle-tracking",
            CancellationToken.None);

        Assert.True(result.ScopeMismatch);
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
        _snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(new ChangeSetSnapshot(
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
        _snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
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
    public async Task GetDevelopmentContextAsync_UsesOnlyDirectTracePathsForScopeExpansion() {
        ChangeSetSnapshot snapshot = new("abc123", "clean-snapshot", [], DateTimeOffset.UtcNow);
        _snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        _snapshots.RefreshAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        _executor.ExecuteAsync("trace", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(CreateResult(
                "trace",
                [
                    "FoodDiary.Application.Users/Commands/UpdateUser.cs",
                    "tests/FoodDiary.ArchitectureTests/TransitiveContext.cs",
                ],
                ["FoodDiary.Application.Users/Commands/UpdateUser.cs"]));
        WikiQueryService service = new(_executor, _snapshots);

        DevelopmentContext result = await service.GetDevelopmentContextAsync(
            "Update user",
            "UpdateUser",
            "FoodDiary.Application.Users",
            CancellationToken.None);

        Assert.False(result.ScopeMismatch);
        Assert.Contains("FoodDiary.Application.Users/Commands/UpdateUser.cs", result.ExpandedScopePaths, StringComparer.Ordinal);
        Assert.DoesNotContain("tests/FoodDiary.ArchitectureTests/TransitiveContext.cs", result.ExpandedScopePaths, StringComparer.Ordinal);
        Assert.False(result.BaselineAvailable);
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
}
