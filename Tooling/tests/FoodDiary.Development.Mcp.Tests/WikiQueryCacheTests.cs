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
