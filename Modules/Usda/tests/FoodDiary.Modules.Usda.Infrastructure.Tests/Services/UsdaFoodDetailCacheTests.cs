using FoodDiary.Integrations.Services;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class UsdaFoodDetailCacheTests {
    [Fact]
    public async Task CapacityOverflow_EvictsEarliestExpiryAndPreservesRecentEntry() {
        var clock = new AdvancingClock();
        var cache = new UsdaFoodDetailCache(clock);
        int calls = 0;
        Task<UsdaFoodDetailLookupResult> Factory(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            calls++;
            return Task.FromResult(new UsdaFoodDetailLookupResult(Cacheable: true, Value: null));
        }

        for (int id = 0; id <= 2048; id++) {
            await cache.GetOrCreateAsync("https://usda.test", id, Factory, CancellationToken.None);
            clock.Now = clock.Now.AddTicks(1);
        }
        Assert.Equal(2049, calls);
        await cache.GetOrCreateAsync("https://usda.test", 2048, Factory, CancellationToken.None);
        Assert.Equal(2049, calls);
        await cache.GetOrCreateAsync("https://usda.test", 0, Factory, CancellationToken.None);
        Assert.Equal(2050, calls);
    }

    [ExcludeFromCodeCoverage]
    private sealed class AdvancingClock : TimeProvider {
        public DateTimeOffset Now { get; set; } = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
