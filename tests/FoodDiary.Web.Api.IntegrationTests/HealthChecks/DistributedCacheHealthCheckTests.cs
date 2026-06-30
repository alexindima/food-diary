using FoodDiary.Web.Api.HealthChecks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodDiary.Web.Api.IntegrationTests.HealthChecks;

[ExcludeFromCodeCoverage]
public sealed class DistributedCacheHealthCheckTests {
    [Fact]
    public async Task CheckHealthAsync_WhenCacheProbeSucceeds_ReturnsHealthyAndRemovesProbeKey() {
        var cache = new CapturingDistributedCache();
        var check = new DistributedCacheHealthCheck(cache, FixedTime);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("health:distributed-cache", cache.SetKey);
        Assert.Equal("health:distributed-cache", cache.RemovedKey);
        Assert.NotNull(cache.SetValue);
        Assert.Equal("1773651600", System.Text.Encoding.UTF8.GetString(cache.SetValue));
        Assert.Equal(TimeSpan.FromSeconds(30), cache.Options?.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCacheProbeFails_ReturnsUnhealthyWithException() {
        var exception = new InvalidOperationException("Cache is down");
        var cache = new CapturingDistributedCache {
            SetAsyncHandler = (_, _, _, _) => throw exception,
        };
        var check = new DistributedCacheHealthCheck(cache, FixedTime);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Distributed cache probe failed.", result.Description);
        Assert.Same(exception, result.Exception);
        Assert.Null(cache.RemovedKey);
    }

    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 3, 16, 9, 0, 0, TimeSpan.Zero);
    }

    [ExcludeFromCodeCoverage]
    private sealed class CapturingDistributedCache : IDistributedCache {
        public Func<string, byte[], DistributedCacheEntryOptions, CancellationToken, Task>? SetAsyncHandler { get; init; }
        public string? SetKey { get; private set; }
        public byte[]? SetValue { get; private set; }
        public DistributedCacheEntryOptions? Options { get; private set; }
        public string? RemovedKey { get; private set; }

        public byte[]? Get(string key) => null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]?>(null);

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) {
            SetKey = key;
            SetValue = value;
            Options = options;
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default) {
            if (SetAsyncHandler is not null) {
                return SetAsyncHandler(key, value, options, token);
            }

            SetKey = key;
            SetValue = value;
            Options = options;
            return Task.CompletedTask;
        }

        public void Refresh(string key) {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) {
            RemovedKey = key;
        }

        public Task RemoveAsync(string key, CancellationToken token = default) {
            RemovedKey = key;
            return Task.CompletedTask;
        }
    }
}
