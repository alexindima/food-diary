using System.Text.Json;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class WikiQueryCacheTests {
    [Fact]
    public void CreateKey_PreservesArgumentBoundariesAndOrder() {
        string splitAfterSecondCharacter = WikiQueryCache.CreateKey(
            "snapshot",
            "brief",
            ["ab", "c"]);
        string splitAfterFirstCharacter = WikiQueryCache.CreateKey(
            "snapshot",
            "brief",
            ["a", "bc"]);
        string reversed = WikiQueryCache.CreateKey(
            "snapshot",
            "brief",
            ["c", "ab"]);

        Assert.False(string.Equals(
            splitAfterSecondCharacter,
            splitAfterFirstCharacter,
            StringComparison.Ordinal));
        Assert.False(string.Equals(splitAfterSecondCharacter, reversed, StringComparison.Ordinal));
    }

    [Fact]
    public void Set_RejectsOversizedResultsAndBoundsEntryCount() {
        WikiRuntimeTelemetry telemetry = new();
        WikiQueryCache cache = new(TimeProvider.System, telemetry);
        WikiCommandResult oversized = CreateResult(new string('x', (1024 * 1024) + 1));

        cache.Set("oversized", "brief", [], oversized);
        Assert.False(cache.TryGet("oversized", "brief", [], out _));

        WikiCommandResult result = CreateResult("{}");
        for (int index = 0; index < 129; index++) {
            cache.Set(index.ToString(System.Globalization.CultureInfo.InvariantCulture), "brief", [], result);
        }

        Assert.Equal(128, cache.CaptureMetrics().QueryCache.Entries);
        Assert.False(cache.TryGet("0", "brief", [], out _));
        Assert.True(cache.TryGet("128", "brief", [], out WikiCommandResult? cached));
        Assert.Same(result, cached);
    }

    [Fact]
    public void CaptureMetrics_PrunesExpiredEntries() {
        ManualTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        WikiQueryCache cache = new(timeProvider, new WikiRuntimeTelemetry());
        cache.Set("snapshot", "brief", [], CreateResult("{}"));

        timeProvider.Advance(TimeSpan.FromMinutes(3));

        Assert.Equal(0, cache.CaptureMetrics().QueryCache.Entries);
        Assert.False(cache.TryGet("snapshot", "brief", [], out _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Set_AfterExpiredKeyIsReinserted_EvictsOldestLiveEntry(bool pruneWithMetrics) {
        ManualTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        WikiQueryCache cache = new(timeProvider, new WikiRuntimeTelemetry());
        WikiCommandResult result = CreateResult("{}");
        cache.Set("reinserted", "brief", [], result);
        timeProvider.Advance(TimeSpan.FromMinutes(3));
        if (pruneWithMetrics) {
            Assert.Equal(0, cache.CaptureMetrics().QueryCache.Entries);
        } else {
            Assert.False(cache.TryGet("reinserted", "brief", [], out _));
        }
        cache.Set("oldest-live", "brief", [], result);
        cache.Set("reinserted", "brief", [], result);
        for (int index = 0; index < 127; index++) {
            cache.Set(index.ToString(System.Globalization.CultureInfo.InvariantCulture), "brief", [], result);
        }

        Assert.Multiple(
            () => Assert.Equal(128, cache.CaptureMetrics().QueryCache.Entries),
            () => Assert.False(cache.TryGet("oldest-live", "brief", [], out _)),
            () => Assert.True(cache.TryGet("reinserted", "brief", [], out _)),
            () => Assert.True(cache.TryGet("126", "brief", [], out _)));
    }

    [Fact]
    public void SetAndTryGet_WithConcurrentRefreshAndMetrics_PreserveLiveResults() {
        WikiQueryCache cache = new(TimeProvider.System, new WikiRuntimeTelemetry());

        Parallel.For(0, 16, worker => {
            string snapshot = worker.ToString(System.Globalization.CultureInfo.InvariantCulture);
            WikiCommandResult expected = CreateResult(snapshot);
            for (int iteration = 0; iteration < 64; iteration++) {
                string argument = (iteration % 8).ToString(System.Globalization.CultureInfo.InvariantCulture);
                cache.Set(snapshot, "brief", [argument], expected);
                Assert.True(cache.TryGet(snapshot, "brief", [argument], out WikiCommandResult? actual));
                Assert.Same(expected, actual);
                Assert.InRange(cache.CaptureMetrics().QueryCache.Entries, 1, 128);
            }
        });

        Assert.Equal(128, cache.CaptureMetrics().QueryCache.Entries);
    }

    private static WikiCommandResult CreateResult(string rawOutput) => new(
        "brief",
        rawOutput,
        JsonSerializer.SerializeToElement(new { }),
        "repository",
        "abc123",
        [],
        [],
        [],
        []);

    [ExcludeFromCodeCoverage]
    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
