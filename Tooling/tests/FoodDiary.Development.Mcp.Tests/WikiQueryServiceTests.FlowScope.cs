using NSubstitute;

namespace FoodDiary.Development.Mcp.Tests;

public sealed partial class WikiQueryServiceTests {
    [Fact]
    public async Task GetDevelopmentContextAsync_TestOnlyFlowQueryDoesNotExpandProductionScope() {
        IWikiContextSearch search = Substitute.For<IWikiContextSearch, IWikiFlowScopeSearch>();
        search.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>(), Arg.Any<string?>())
            .Returns(CreateSqlContext(ready: true, fresh: true));
        await new WikiQueryService(_executor, _snapshots, contextSearch: search)
            .GetDevelopmentContextAsync("Inspect tests", "tests for the stock reservation flow", plannedPath: null, CancellationToken.None);
        await ((IWikiFlowScopeSearch)search).DidNotReceive().ExpandFlowScopeAsync(Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetDevelopmentContextAsync_UsesFreshFlowScopeAndReportsUnavailableExpansion(bool ready) {
        IWikiContextSearch search = Substitute.For<IWikiContextSearch, IWikiFlowScopeSearch>();
        search.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>(), Arg.Any<string?>())
            .Returns(CreateSqlContext(ready: true, fresh: true));
        const string worker = "FoodDiary.JobManager/Services/StockReservationWorker.cs";
        ((IWikiFlowScopeSearch)search).ExpandFlowScopeAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DevelopmentFlowScope(ready, [worker], [], ["endpoint"], Truncated: false,
                DurationMilliseconds: 1, UnavailableReason: ready ? null : "snapshot-mismatch"));

        DevelopmentContext result = await new WikiQueryService(_executor, _snapshots, contextSearch: search)
            .GetDevelopmentContextAsync("Investigate stock reservation", "Trace the stock reservation flow and tests", plannedPath: null, CancellationToken.None);

        Assert.NotNull(result.FlowScope);
        Assert.Equal(ready, result.FlowScope.Ready);
        Assert.Equal(!ready, result.PartialSuccess);
        if (ready) {
            Assert.Contains(worker, result.ExpandedScopePaths, StringComparer.Ordinal);
            await _executor.Received(1).ExecuteAsync("test-plan", Arg.Is<IReadOnlyList<string>>(args => args.Contains(worker)), CancellationToken.None);
        } else {
            Assert.DoesNotContain(worker, result.ExpandedScopePaths, StringComparer.Ordinal);
            Assert.Contains(result.ComponentErrors, error => string.Equals(error.Component, "flow-scope", StringComparison.Ordinal));
        }
        await _executor.DidNotReceive().ExecuteAsync("trace", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }
}
