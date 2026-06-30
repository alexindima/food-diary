using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodDiary.Web.Api.HealthChecks;

public sealed class DistributedCacheHealthCheck(IDistributedCache cache, TimeProvider timeProvider) : IHealthCheck {
    private const string ProbeKey = "health:distributed-cache";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        try {
            await cache.SetStringAsync(
                ProbeKey,
                timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                new DistributedCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
                },
                cancellationToken).ConfigureAwait(false);
            await cache.RemoveAsync(ProbeKey, cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        } catch (Exception exception) {
            return HealthCheckResult.Unhealthy("Distributed cache probe failed.", exception);
        }
    }
}
