namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class WikiRuntimeTelemetryTests {
    [Fact]
    public void Capture_ReportsBoundedCommandPercentilesAndOutcomes() {
        WikiRuntimeTelemetry telemetry = new();
        telemetry.RecordCacheMiss();
        telemetry.RecordCacheHit();

        Complete(telemetry, "brief", 10);
        Complete(telemetry, "brief", 20);
        Complete(telemetry, "brief", 100);
        telemetry.RecordCommandStage("brief", "process-round-trip", TimeSpan.FromMilliseconds(8));
        telemetry.RecordCommandStage("brief", "process-round-trip", TimeSpan.FromMilliseconds(80));
        telemetry.CommandQueued();
        telemetry.CommandStarted();
        telemetry.CommandFailed(cancelled: false, timedOut: true);
        telemetry.CommandQueued();
        telemetry.CommandQueueCancelled();

        WikiRuntimeMetrics metrics = telemetry.Capture(cacheEntries: 2);

        Assert.Equal(2, metrics.QueryCache.Entries);
        Assert.Equal(0.5, metrics.QueryCache.HitRate);
        Assert.Equal(3, metrics.CompletedCommands);
        Assert.Equal(1, metrics.TimedOutCommands);
        Assert.Equal(1, metrics.CancelledCommands);
        Assert.Equal(0, metrics.ActiveCommands);
        Assert.Equal(0, metrics.QueuedCommands);
        WikiCommandTiming timing = Assert.Single(metrics.CommandTimings);
        Assert.Equal("brief", timing.Command);
        Assert.Equal(3, timing.Samples);
        Assert.Equal(20, timing.P50Milliseconds);
        Assert.Equal(100, timing.P95Milliseconds);
        Assert.Equal(100, timing.MaximumMilliseconds);
        WikiCommandStageTiming stageTiming = Assert.Single(metrics.CommandStageTimings);
        Assert.Equal("brief", stageTiming.Command);
        Assert.Equal("process-round-trip", stageTiming.Stage);
        Assert.Equal(2, stageTiming.Samples);
        Assert.Equal(8, stageTiming.P50Milliseconds);
        Assert.Equal(80, stageTiming.P95Milliseconds);
        Assert.Equal(80, stageTiming.MaximumMilliseconds);
    }

    private static void Complete(
        WikiRuntimeTelemetry telemetry,
        string command,
        double milliseconds) {
        telemetry.CommandQueued();
        telemetry.CommandStarted();
        telemetry.CommandCompleted(command, TimeSpan.FromMilliseconds(milliseconds));
    }
}
