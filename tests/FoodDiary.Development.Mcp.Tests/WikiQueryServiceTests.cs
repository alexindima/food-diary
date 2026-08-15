using NSubstitute;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class WikiQueryServiceTests {
    private readonly IWikiCommandExecutor _executor = Substitute.For<IWikiCommandExecutor>();
    private readonly IChangeSetSnapshotService _snapshots = Substitute.For<IChangeSetSnapshotService>();

    public WikiQueryServiceTests() {
        _snapshots.GetAsync(Arg.Any<CancellationToken>()).Returns(new ChangeSetSnapshot(
            "abc123",
            "snapshot-hash",
            ["FoodDiary.Development.Mcp/WikiQueryService.cs"],
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task GetChangeContextAsync_PassesIntentAndPlannedPathAsSeparateArguments() {
        WikiQueryService service = new(_executor, _snapshots);
        CancellationToken cancellationToken = new();

        await service.GetChangeContextAsync(
            "Add MCP; Write-Error must stay data",
            "FoodDiary.Development.Mcp",
            cancellationToken);

        await _executor.Received(1).ExecuteAsync(
            "brief",
            Arg.Is<IReadOnlyList<string>>(arguments =>
                arguments.SequenceEqual(new[] {
                    "-Format",
                    "Json",
                    "-Intent",
                    "Add MCP; Write-Error must stay data",
                    "-PlannedPath",
                    "FoodDiary.Development.Mcp",
                    "-ChangedPathList",
                    "FoodDiary.Development.Mcp/WikiQueryService.cs",
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

        await service.GetTestPlanAsync(intent: null, CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.SequenceEqual(new[] {
                "-Format",
                "Json",
                "-ChangedPathList",
                "FoodDiary.Development.Mcp/WikiQueryService.cs",
            })),
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
                "-Intent",
                "Change a backend flow",
                "-ChangedPathList",
                "FoodDiary.Development.Mcp/WikiQueryService.cs",
            })),
            CancellationToken.None);
    }
}
