namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
[Collection("PowerShell Wiki process")]
public sealed class PowerShellWikiCommandExecutorTests {
    [Fact]
    public async Task ServerStatus_ReturnsWithoutRunningDeepVerification() {
        var runtimeIdentity = ServerRuntimeIdentity.Capture("startup-head");
        using ChangeSetSnapshotService snapshots = new(TimeProvider.System);
        ServerStatusService service = new(TimeProvider.System, runtimeIdentity, snapshots);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));

        ServerStatus result = await service.GetStatusAsync(timeout.Token);

        Assert.True(result.WikiAvailable);
        Assert.True(new[] { "verified", "unverified", "stale" }.Contains(result.DeepFreshness, StringComparer.Ordinal));
        Assert.Matches("^[a-f0-9]{64}$", result.IndexFingerprint);
        Assert.Equal(result.IndexesMatchWorktree, string.Equals(result.DeepFreshness, "verified", StringComparison.Ordinal));
        Assert.Equal(Environment.ProcessId, result.RuntimeIdentity.ProcessId);
        Assert.Equal("startup-head", result.RuntimeIdentity.RepositoryHeadAtStartup);
        Assert.False(result.RunningCodeMatchesRepositoryHead);
        Assert.NotEmpty(result.WorktreeFingerprint);
        Assert.Contains(result.Indexes, index => index.Path.EndsWith("runtime-topology.json", StringComparison.Ordinal));
        Assert.Contains(result.Indexes, index => index.Path.EndsWith("sensitive-data-index.json", StringComparison.Ordinal));
        Assert.Contains(result.Indexes, index => index.Path.EndsWith("frontend-index.json", StringComparison.Ordinal));
        Assert.Equal(0, result.RuntimeMetrics.QueryCache.Entries);
        Assert.Equal(0, result.RuntimeMetrics.ActiveCommands);
        Assert.Equal(0, result.RuntimeMetrics.ContextRouting.SampleCount);
        Assert.True(result.RuntimeMetrics.ContextRouting.PersistenceHealthy);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFocusedTestPlan() {
        WikiRuntimeTelemetry telemetry = new();
        PowerShellWikiCommandExecutor executor = new(telemetry);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(90));

        WikiCommandResult result = await executor.ExecuteAsync(
            "test-plan",
            [
                "-Format",
                "Json",
                "-Fast",
                "-Objective",
                "Verify the FoodDiary Development MCP test plan tool",
                "-ChangedPath",
                "FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs",
            ],
            timeout.Token);

        Assert.StartsWith("{", result.RawOutput, StringComparison.Ordinal);
        WikiRuntimeMetrics metrics = telemetry.Capture(cacheEntries: 0);
        Assert.Collection(
            metrics.CommandStageTimings.OrderBy(timing => timing.Stage, StringComparer.Ordinal),
            timing => AssertStage(timing, "process-round-trip"),
            timing => AssertStage(timing, "queue-wait"),
            timing => AssertStage(timing, "request-serialization"),
            timing => AssertStage(timing, "result-processing"));
    }

    [Fact]
    public async Task ExecuteAsync_UsesRequestFileForLongUnicodeScope() {
        PowerShellWikiCommandExecutor executor = new();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));
        List<string> arguments = ["-Format", "Json", "-Fast", "-Objective", new string('я', 12_000)];
        for (int index = 0; index < 250; index++) {
            arguments.Add("-ChangedPath");
            arguments.Add($"FoodDiary.Web.Client/src/app/измерения/компонент-{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}.ts");
        }

        WikiCommandResult result = await executor.ExecuteAsync(
            "test-plan",
            arguments,
            timeout.Token);

        Assert.StartsWith("{", result.RawOutput, StringComparison.Ordinal);
    }

    private static void AssertStage(WikiCommandStageTiming timing, string expectedStage) {
        Assert.Equal("test-plan", timing.Command);
        Assert.Equal(expectedStage, timing.Stage);
        Assert.Equal(1, timing.Samples);
        Assert.True(timing.MaximumMilliseconds >= 0);
    }
}
