using NSubstitute;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class WikiQueryServiceTests {
    private readonly IWikiCommandExecutor _executor = Substitute.For<IWikiCommandExecutor>();

    [Fact]
    public async Task GetChangeContextAsync_PassesIntentAndPlannedPathAsSeparateArguments() {
        WikiQueryService service = new(_executor);
        CancellationToken cancellationToken = new();

        await service.GetChangeContextAsync(
            "Add MCP; Write-Error must stay data",
            "FoodDiary.Development.Mcp",
            cancellationToken);

        await _executor.Received(1).ExecuteAsync(
            "brief",
            Arg.Is<IReadOnlyList<string>>(arguments =>
                arguments.SequenceEqual(new[] {
                    "-Intent",
                    "Add MCP; Write-Error must stay data",
                    "-PlannedPath",
                    "FoodDiary.Development.Mcp",
                })),
            cancellationToken);
    }

    [Fact]
    public async Task TraceBackendFlowAsync_RejectsBlankQuery() {
        WikiQueryService service = new(_executor);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.TraceBackendFlowAsync(" ", CancellationToken.None));
    }

    [Fact]
    public async Task GetTestPlanAsync_UsesCurrentChangeSetWhenQueryIsMissing() {
        WikiQueryService service = new(_executor);

        await service.GetTestPlanAsync(intent: null, CancellationToken.None);

        await _executor.Received(1).ExecuteAsync(
            "test-plan",
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.Count == 0),
            CancellationToken.None);
    }
}
