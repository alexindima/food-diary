using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Usda.Infrastructure.Providers.Services;

namespace FoodDiary.Modules.Usda.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class UsdaFoodDetailCacheTests {
    [Theory]
    [InlineData(true, 30)]
    [InlineData(false, 5)]
    public async Task CacheableLookup_ExpiresAtItsConfiguredDeadline(bool found, int lifetimeMinutes) {
        var clock = new AdvancingClock();
        var cache = new UsdaFoodDetailCache(clock);
        var food = new UsdaFoodDetailModel(42, "Apple", FoodCategory: null, [], [], HealthScores: null);
        int calls = 0;

        Task<UsdaFoodDetailLookupResult> Lookup(CancellationToken _) {
            calls++;
            return Task.FromResult<UsdaFoodDetailLookupResult>(
                found ? new UsdaFoodDetailLookupResult.Found(food) : new UsdaFoodDetailLookupResult.NotFound());
        }

        UsdaFoodDetailModel? first = await cache.GetOrCreateAsync("https://usda.test", 42, Lookup, CancellationToken.None);
        clock.Now = clock.Now.AddMinutes(lifetimeMinutes).AddTicks(-1);
        UsdaFoodDetailModel? cached = await cache.GetOrCreateAsync("https://usda.test", 42, Lookup, CancellationToken.None);
        Assert.Equal(1, calls);
        clock.Now = clock.Now.AddTicks(1);
        await cache.GetOrCreateAsync("https://usda.test", 42, Lookup, CancellationToken.None);

        Assert.Multiple(
            () => Assert.Same(found ? food : null, first),
            () => Assert.Same(first, cached),
            () => Assert.Equal(2, calls));
    }

    [Fact]
    public async Task QueuedSameKeyCallers_ShareNewFlightAndReturnSurplusAdmissionPermit() {
        var cache = new UsdaFoodDetailCache(TimeProvider.System, 2, TimeSpan.FromSeconds(30));
        var firstGate = new TaskCompletionSource<UsdaFoodDetailLookupResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondGate = new TaskCompletionSource<UsdaFoodDetailLookupResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sharedGate = new TaskCompletionSource<UsdaFoodDetailLookupResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sharedEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sentinelEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int sharedCalls = 0;
        UsdaFoodDetailLookupResult lookup = new UsdaFoodDetailLookupResult.LookupFailed();
        Task<UsdaFoodDetailLookupResult> Shared(CancellationToken token) {
            Interlocked.Increment(ref sharedCalls);
            sharedEntered.TrySetResult();
            return sharedGate.Task.WaitAsync(token);
        }
        Task first = cache.GetOrCreateAsync("https://usda.test", 1, token => firstGate.Task.WaitAsync(token), CancellationToken.None);
        Task second = cache.GetOrCreateAsync("https://usda.test", 2, token => secondGate.Task.WaitAsync(token), CancellationToken.None);
        Task sharedFirst = cache.GetOrCreateAsync("https://usda.test", 3, Shared, CancellationToken.None);
        Task sharedSecond = cache.GetOrCreateAsync("https://usda.test", 3, Shared, CancellationToken.None);
        Task sentinel = cache.GetOrCreateAsync("https://usda.test", 4, _ => {
            sentinelEntered.TrySetResult();
            return Task.FromResult(lookup);
        }, CancellationToken.None);
        try {
            firstGate.SetResult(lookup);
            await sharedEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            secondGate.SetResult(lookup);
            // The queued sentinel can enter only after the second shared caller returns its surplus permit.
            await sentinelEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            Assert.False(sharedFirst.IsCompleted);
            Assert.False(sharedSecond.IsCompleted);
            sharedGate.SetResult(lookup);
            await Task.WhenAll(first, second, sharedFirst, sharedSecond, sentinel).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            Assert.Equal(1, sharedCalls);
        } finally {
            firstGate.TrySetResult(lookup);
            secondGate.TrySetResult(lookup);
            sharedGate.TrySetResult(lookup);
        }
    }
    [Fact]
    public async Task CapacityOverflow_EvictsEarliestExpiryAndPreservesRecentEntry() {
        var clock = new AdvancingClock();
        var cache = new UsdaFoodDetailCache(clock);
        int calls = 0;
        Task<UsdaFoodDetailLookupResult> Factory(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            calls++;
            return Task.FromResult<UsdaFoodDetailLookupResult>(new UsdaFoodDetailLookupResult.NotFound());
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
