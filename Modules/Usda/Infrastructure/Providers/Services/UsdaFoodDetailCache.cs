using System.Collections.Concurrent;
using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Integrations.Services;

internal sealed class UsdaFoodDetailCache {
    private const int MaximumEntries = 2048;
    private const int DefaultMaximumInFlightEntries = 8;
    private static readonly TimeSpan PositiveDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan NegativeDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultSharedOperationTimeout = TimeSpan.FromSeconds(20);
    private readonly ConcurrentDictionary<CacheKey, CacheEntry> _entries = new();
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<UsdaFoodDetailModel?>>> _inFlight = new();
    private readonly SemaphoreSlim _inFlightAdmission;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _sharedOperationTimeout;

    public UsdaFoodDetailCache(TimeProvider timeProvider)
        : this(timeProvider, DefaultMaximumInFlightEntries, DefaultSharedOperationTimeout) {
    }

    internal UsdaFoodDetailCache(
        TimeProvider timeProvider,
        int maximumInFlightEntries,
        TimeSpan sharedOperationTimeout) {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumInFlightEntries, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sharedOperationTimeout, TimeSpan.Zero);

        _timeProvider = timeProvider;
        _sharedOperationTimeout = sharedOperationTimeout;
        _inFlightAdmission = new SemaphoreSlim(maximumInFlightEntries, maximumInFlightEntries);
    }

    public async Task<UsdaFoodDetailModel?> GetOrCreateAsync(
        string baseUrl,
        int fdcId,
        Func<CancellationToken, Task<UsdaFoodDetailLookupResult>> factory,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var key = new CacheKey(baseUrl, fdcId);
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (_entries.TryGetValue(key, out CacheEntry? cached) && cached.ExpiresAt > now) {
            return cached.Value;
        }

        _entries.TryRemove(key, out _);
        Lazy<Task<UsdaFoodDetailModel?>>? pending;
        while (!_inFlight.TryGetValue(key, out pending)) {
            await _inFlightAdmission.WaitAsync(cancellationToken).ConfigureAwait(false);
            lock (_inFlight) {
                if (_inFlight.TryGetValue(key, out pending)) {
                    _inFlightAdmission.Release();
                    break;
                }

                pending = new Lazy<Task<UsdaFoodDetailModel?>>(
                    () => CreateAndCacheAsync(key, factory),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                _inFlight[key] = pending;
            }
        }

        Task<UsdaFoodDetailModel?> pendingTask = pending.Value;
        _ = pendingTask.ContinueWith(
            static (completedTask, state) => {
                _ = completedTask.Exception;
                (UsdaFoodDetailCache cache, CacheKey cacheKey, Lazy<Task<UsdaFoodDetailModel?>> expected) =
                    ((UsdaFoodDetailCache, CacheKey, Lazy<Task<UsdaFoodDetailModel?>>))state!;
                if (cache._inFlight.TryRemove(
                        new KeyValuePair<CacheKey, Lazy<Task<UsdaFoodDetailModel?>>>(cacheKey, expected))) {
                    cache._inFlightAdmission.Release();
                }
            },
            (this, key, pending),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return await pendingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<UsdaFoodDetailModel?> CreateAndCacheAsync(
        CacheKey key,
        Func<CancellationToken, Task<UsdaFoodDetailLookupResult>> factory) {
        using var sharedDeadline = new CancellationTokenSource(_sharedOperationTimeout, _timeProvider);
        UsdaFoodDetailLookupResult result;
        try {
            result = await factory(sharedDeadline.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (sharedDeadline.IsCancellationRequested) {
            return null;
        }

        if (result.Cacheable) {
            TimeSpan duration = result.Value is null ? NegativeDuration : PositiveDuration;
            _entries[key] = new CacheEntry(result.Value, _timeProvider.GetUtcNow().Add(duration));
            TrimIfNeeded();
        }

        return result.Value;
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
