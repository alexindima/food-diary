using System.Collections.Concurrent;
using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Integrations.Services;

internal sealed class UsdaFoodDetailCache(TimeProvider timeProvider) {
    private const int MaximumEntries = 2048;
    private static readonly TimeSpan PositiveDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan NegativeDuration = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<CacheKey, CacheEntry> _entries = new();
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<UsdaFoodDetailLookupResult>>> _inFlight = new();

    public async Task<UsdaFoodDetailModel?> GetOrCreateAsync(
        string baseUrl,
        int fdcId,
        Func<Task<UsdaFoodDetailLookupResult>> factory,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var key = new CacheKey(baseUrl, fdcId);
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (_entries.TryGetValue(key, out CacheEntry? cached) && cached.ExpiresAt > now) {
            return cached.Value;
        }

        _entries.TryRemove(key, out _);
        Lazy<Task<UsdaFoodDetailLookupResult>> pending = _inFlight.GetOrAdd(
            key,
            static (_, factoryArgument) => new Lazy<Task<UsdaFoodDetailLookupResult>>(
                factoryArgument,
                LazyThreadSafetyMode.ExecutionAndPublication),
            factory);

        try {
            UsdaFoodDetailLookupResult result = await pending.Value.ConfigureAwait(false);
            if (result.Cacheable) {
                TimeSpan duration = result.Value is null ? NegativeDuration : PositiveDuration;
                _entries[key] = new CacheEntry(result.Value, timeProvider.GetUtcNow().Add(duration));
                TrimIfNeeded();
            }

            return result.Value;
        } finally {
            _inFlight.TryRemove(new KeyValuePair<CacheKey, Lazy<Task<UsdaFoodDetailLookupResult>>>(key, pending));
        }
    }

    private void TrimIfNeeded() {
        if (_entries.Count <= MaximumEntries) {
            return;
        }

        foreach (CacheKey key in _entries
                     .OrderBy(static pair => pair.Value.ExpiresAt)
                     .Take(_entries.Count - MaximumEntries)
                     .Select(static pair => pair.Key)) {
            _entries.TryRemove(key, out _);
        }
    }

    private readonly record struct CacheKey(string BaseUrl, int FdcId);
    private sealed record CacheEntry(UsdaFoodDetailModel? Value, DateTimeOffset ExpiresAt);
}
