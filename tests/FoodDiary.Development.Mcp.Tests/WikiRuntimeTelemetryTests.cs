namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class WikiRuntimeTelemetryTests {
    [Fact]
    public void Capture_WithNoCacheAttemptsAndRegularFailure_ReturnsZeroRateAndFailureCount() {
        WikiRuntimeTelemetry telemetry = new();
        telemetry.CommandQueued();
        telemetry.CommandStarted();
        telemetry.CommandFailed(cancelled: false, timedOut: false);
        for (int index = 0; index < 140; index++) {
            Complete(telemetry, "bounded", index);
        }

        WikiRuntimeMetrics metrics = telemetry.Capture(cacheEntries: 0);

        WikiCommandTiming timing = Assert.Single(metrics.CommandTimings);
        Assert.Multiple(
            () => Assert.Equal(0, metrics.QueryCache.HitRate),
            () => Assert.Equal(1, metrics.FailedCommands),
            () => Assert.Equal(128, timing.Samples),
            () => Assert.Equal(139, timing.MaximumMilliseconds));
    }

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
        Assert.Equal(ContextRoutingHealth.Empty, metrics.ContextRouting);
    }

    [Fact]
    public void ContextRoutingTelemetry_PersistsBoundedPrivacySafeHealthAcrossInstances() {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "fooddiary-development-mcp-tests",
            Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "context-routing.json");
        try {
            ContextRoutingTelemetryStore store = new(path, maximumEvents: 3);
            store.Record(
                outcome: ContextRoutingOutcome.SqlitePrimary,
                fallbackReason: null,
                TimeSpan.FromMilliseconds(10),
                refreshAttempted: false,
                refreshSucceeded: false);
            store.Record(
                outcome: ContextRoutingOutcome.JsonFallback,
                fallbackReason: "graph-refresh-secret-query-and-path",
                TimeSpan.FromMilliseconds(40),
                refreshAttempted: true,
                refreshSucceeded: false);
            store.Record(
                outcome: ContextRoutingOutcome.JsonFallback,
                fallbackReason: "sqlite-no-candidates",
                TimeSpan.FromMilliseconds(20),
                refreshAttempted: false,
                refreshSucceeded: false);
            store.Record(
                outcome: ContextRoutingOutcome.SqlitePrimary,
                fallbackReason: null,
                TimeSpan.FromMilliseconds(30),
                refreshAttempted: false,
                refreshSucceeded: false);

            ContextRoutingHealth health = new ContextRoutingTelemetryStore(path, maximumEvents: 3).Capture();

            Assert.Equal(3, health.SampleCount);
            Assert.Equal(1, health.SqlitePrimaryCount);
            Assert.Equal(2, health.JsonFallbackCount);
            Assert.Equal(0.6667, health.JsonFallbackRate);
            Assert.Equal(1, health.SqliteNoCandidateFallbackCount);
            Assert.Equal(30, health.P50Milliseconds);
            Assert.Equal(40, health.P95Milliseconds);
            Assert.Equal(1, health.FallbackReasonCounts["graph-refresh-failed"]);
            Assert.Equal(1, health.RefreshAttemptCount);
            Assert.Equal(0, health.RefreshSuccessCount);
            Assert.Equal(1, health.RefreshFailureCount);
            Assert.Equal(1, health.ConsecutiveSqlitePrimaryCount);
            Assert.Equal(0.01, health.MaximumRetirementJsonFallbackRate);
            Assert.Equal(200, health.RequiredRetirementSampleCount);
            Assert.Equal(197, health.MinimumAdditionalSqlitePrimarySamplesRequired);
            Assert.True(health.PersistenceHealthy);
            Assert.False(health.JsonFallbackRetirementReady);
            string persisted = File.ReadAllText(path);
            Assert.DoesNotContain("secret-query", persisted, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("query-and-path", persisted, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("consecutiveSqlitePrimaryCount", persisted, StringComparison.Ordinal);
            Assert.DoesNotContain("minimumAdditionalSqlitePrimarySamplesRequired", persisted, StringComparison.Ordinal);
        } finally {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ContextRoutingTelemetry_SerializesConcurrentWritersAndReportsRetirementEvidence() {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "fooddiary-development-mcp-tests",
            Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "context-routing.json");
        try {
            ContextRoutingTelemetryStore store = new(path, maximumEvents: 100);
            await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => store.Record(
                outcome: ContextRoutingOutcome.SqlitePrimary,
                fallbackReason: null,
                TimeSpan.FromMilliseconds(5),
                refreshAttempted: false,
                refreshSucceeded: false))));

            ContextRoutingHealth health = store.Capture();

            Assert.Equal(100, health.SampleCount);
            Assert.Equal(100, health.SqlitePrimaryCount);
            Assert.Equal(0, health.JsonFallbackCount);
            Assert.Equal(100, health.ConsecutiveSqlitePrimaryCount);
            Assert.Equal(100, health.RequiredRetirementSampleCount);
            Assert.Equal(0, health.MinimumAdditionalSqlitePrimarySamplesRequired);
            Assert.True(health.JsonFallbackRetirementReady);
            Assert.Equal(0, health.PersistenceFailures);
        } finally {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ContextRoutingTelemetry_ReportsMinimumSuccessfulSamplesNeededForRetirement() {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "fooddiary-development-mcp-tests",
            Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "context-routing.json");
        try {
            ContextRoutingTelemetryStore store = new(path, maximumEvents: 1000);
            store.Record(
                outcome: ContextRoutingOutcome.JsonFallback,
                fallbackReason: "graph-refresh-failed",
                TimeSpan.FromMilliseconds(40),
                refreshAttempted: true,
                refreshSucceeded: false);
            foreach (int duration in Enumerable.Range(1, 60)) {
                store.Record(
                    outcome: ContextRoutingOutcome.SqlitePrimary,
                    fallbackReason: null,
                    TimeSpan.FromMilliseconds(duration),
                    refreshAttempted: false,
                    refreshSucceeded: false);
            }

            ContextRoutingHealth health = store.Capture();

            Assert.Multiple(
                () => Assert.Equal(61, health.SampleCount),
                () => Assert.Equal(60, health.SqlitePrimaryCount),
                () => Assert.Equal(1, health.JsonFallbackCount),
                () => Assert.Equal(0.0164, health.JsonFallbackRate),
                () => Assert.Equal(60, health.ConsecutiveSqlitePrimaryCount),
                () => Assert.Equal(1, health.RefreshAttemptCount),
                () => Assert.Equal(0, health.RefreshSuccessCount),
                () => Assert.Equal(1, health.RefreshFailureCount),
                () => Assert.Equal(100, health.RequiredRetirementSampleCount),
                () => Assert.Equal(39, health.MinimumAdditionalSqlitePrimarySamplesRequired),
                () => Assert.False(health.JsonFallbackRetirementReady));
        } finally {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ContextRoutingTelemetry_DistinguishesSqliteUnavailableFromHistoricalJsonFallback() {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "fooddiary-development-mcp-tests",
            Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "context-routing.json");
        try {
            ContextRoutingTelemetryStore store = new(path);
            store.Record(
                ContextRoutingOutcome.SqliteUnavailable,
                fallbackReason: "sqlite-error-5",
                TimeSpan.FromMilliseconds(20),
                refreshAttempted: false,
                refreshSucceeded: false);

            ContextRoutingHealth health = store.Capture();

            Assert.Multiple(
                () => Assert.Equal(1, health.SampleCount),
                () => Assert.Equal(0, health.SqlitePrimaryCount),
                () => Assert.Equal(1, health.SqliteUnavailableCount),
                () => Assert.Equal(1, health.SqliteUnavailableRate),
                () => Assert.Equal(0, health.JsonFallbackCount),
                () => Assert.Equal(0, health.JsonFallbackRate),
                () => Assert.Equal(1, health.FallbackReasonCounts["sqlite-error"]));
            string persisted = File.ReadAllText(path);
            Assert.Contains("sqlite-unavailable", persisted, StringComparison.Ordinal);
            Assert.DoesNotContain("json-fallback", persisted, StringComparison.Ordinal);
        } finally {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(null, "other")]
    [InlineData(" ", "other")]
    [InlineData("sqlite-error-11", "sqlite-error")]
    [InlineData("database-missing", "database-missing")]
    [InlineData("fts-projection-not-ready", "fts-projection-not-ready")]
    [InlineData("snapshot-mismatch", "snapshot-mismatch")]
    [InlineData("sqlite-reader-not-configured", "sqlite-reader-not-configured")]
    [InlineData("private-query", "other")]
    public void ContextRoutingTelemetry_NormalizesFallbackReasons(string? reason, string expected) {
        string directory = Path.Combine(Path.GetTempPath(), "fooddiary-development-mcp-tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "context-routing.json");
        try {
            var store = new ContextRoutingTelemetryStore(path);
            store.Record(
                outcome: ContextRoutingOutcome.JsonFallback,
                fallbackReason: reason,
                duration: TimeSpan.FromMilliseconds(-5),
                refreshAttempted: false,
                refreshSucceeded: false);

            ContextRoutingHealth health = store.Capture();

            Assert.Multiple(
                () => Assert.Equal(0, health.P50Milliseconds),
                () => Assert.Equal(1, health.FallbackReasonCounts[expected]));
        } finally {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ContextRoutingTelemetry_WithInvalidPersistedJson_ReportsPersistenceFailure() {
        string directory = Path.Combine(Path.GetTempPath(), "fooddiary-development-mcp-tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "context-routing.json");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, "not-json");
        try {
            var store = new ContextRoutingTelemetryStore(path);

            ContextRoutingHealth health = store.Capture();

            Assert.Multiple(
                () => Assert.False(health.PersistenceHealthy),
                () => Assert.Equal(1, health.PersistenceFailures),
                () => Assert.NotNull(health.LastPersistenceFailureAtUtc));
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ContextRoutingTelemetry_WithMissingPath_Throws(string path) {
        Assert.Throws<ArgumentException>(() => new ContextRoutingTelemetryStore(path));
    }

    [Fact]
    public void ContextRoutingTelemetry_WithInvalidRetention_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ContextRoutingTelemetryStore("context-routing.json", maximumEvents: 0));
    }

    [Fact]
    public async Task ContextRoutingTelemetry_WhenMutexIsBusy_RecordsTimeoutFailure() {
        string directory = Path.Combine(Path.GetTempPath(), "fooddiary-development-mcp-tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "context-routing.json");
        string mutexName = CreateTelemetryMutexName(path);
        using ManualResetEventSlim acquired = new();
        using ManualResetEventSlim release = new();
        var holder = Task.Run(() => {
            using Mutex mutex = new(initiallyOwned: false, mutexName);
            mutex.WaitOne();
            acquired.Set();
            release.Wait();
            mutex.ReleaseMutex();
        });
        Assert.True(acquired.Wait(TimeSpan.FromSeconds(5)));
        try {
            var store = new ContextRoutingTelemetryStore(path);

            store.Record(
                outcome: ContextRoutingOutcome.SqlitePrimary,
                fallbackReason: null,
                duration: TimeSpan.Zero,
                refreshAttempted: false,
                refreshSucceeded: false);

            ContextRoutingHealth health = store.Capture();
            Assert.Multiple(
                () => Assert.Equal(2, health.PersistenceFailures),
                () => Assert.NotNull(health.LastPersistenceFailureAtUtc));
        } finally {
            release.Set();
            await holder;
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ContextRoutingTelemetry_WhenMutexIsAbandoned_ContinuesSafely() {
        string directory = Path.Combine(Path.GetTempPath(), "fooddiary-development-mcp-tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "context-routing.json");
        string mutexName = CreateTelemetryMutexName(path);
        using ManualResetEventSlim acquired = new();
        var thread = new Thread(() => {
            using Mutex mutex = new(initiallyOwned: false, mutexName);
            mutex.WaitOne();
            acquired.Set();
        });
        thread.Start();
        Assert.True(acquired.Wait(TimeSpan.FromSeconds(5)));
        thread.Join();
        try {
            var store = new ContextRoutingTelemetryStore(path);

            ContextRoutingHealth health = store.Capture();

            Assert.True(health.PersistenceHealthy);
        } finally {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static string CreateTelemetryMutexName(string path) {
        string fullPath = Path.GetFullPath(path);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(fullPath.ToUpperInvariant()));
        return $"FoodDiary.LlmWiki.ContextRouting.{Convert.ToHexString(hash)}";
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
