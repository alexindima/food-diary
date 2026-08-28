namespace FoodDiary.Presentation.Api.Filters;

public sealed class InMemoryIdempotencyStore(TimeProvider timeProvider) : IIdempotencyStore {
    private const int DefaultMaximumEntries = 10_000;
    private readonly Lock _syncRoot = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly PriorityQueue<Expiration, DateTime> _expirations = new();
    private readonly int _maximumEntries = DefaultMaximumEntries;

    public InMemoryIdempotencyStore(TimeProvider timeProvider, int maximumEntries) : this(timeProvider) {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumEntries, 1);
        _maximumEntries = maximumEntries;
    }

    public Task<IdempotencyReservation> ReserveAsync(
        string key,
        string requestHash,
        TimeSpan responseTtl,
        TimeSpan processingTtl,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        lock (_syncRoot) {
            RemoveExpiredEntries(nowUtc);

            if (_entries.TryGetValue(key, out Entry? entry)) {
                if (!string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal)) {
                    return Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.Conflict));
                }

                if (entry.Completed) {
                    return Task.FromResult(new IdempotencyReservation(
                        IdempotencyReservationStatus.Replay,
                        entry.StatusCode,
                        entry.Body,
                        entry.Location));
                }

                return Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.InProgress));
            }

            if (_entries.Count >= _maximumEntries) {
                return Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.CapacityExceeded));
            }

            string ownerToken = Guid.NewGuid().ToString("N");
            DateTime expiresAtUtc = nowUtc.Add(processingTtl);
            _entries[key] = Entry.InProgress(requestHash, ownerToken, expiresAtUtc);
            _expirations.Enqueue(new Expiration(key, expiresAtUtc), expiresAtUtc);
            return Task.FromResult(new IdempotencyReservation(
                IdempotencyReservationStatus.Acquired,
                OwnerToken: ownerToken));
        }
    }

    public Task<bool> RenewAsync(
        string key,
        string requestHash,
        string ownerToken,
        TimeSpan processingTtl,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        lock (_syncRoot) {
            RemoveExpiredEntries(nowUtc);
            if (!_entries.TryGetValue(key, out Entry? entry) ||
                entry.Completed ||
                !string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal) ||
                !string.Equals(entry.OwnerToken, ownerToken, StringComparison.Ordinal)) {
                return Task.FromResult(false);
            }

            DateTime expiresAtUtc = nowUtc.Add(processingTtl);
            _entries[key] = entry with { ExpiresAtUtc = expiresAtUtc };
            _expirations.Enqueue(new Expiration(key, expiresAtUtc), expiresAtUtc);
            CompactExpirationQueueIfNeeded();
            return Task.FromResult(true);
        }
    }

    public Task<bool> CompleteAsync(
        string key,
        string requestHash,
        string ownerToken,
        int statusCode,
        string? body,
        string? location,
        TimeSpan responseTtl,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        lock (_syncRoot) {
            RemoveExpiredEntries(nowUtc);
            if (_entries.TryGetValue(key, out Entry? entry) &&
                !entry.Completed &&
                string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal) &&
                string.Equals(entry.OwnerToken, ownerToken, StringComparison.Ordinal)) {
                DateTime expiresAtUtc = nowUtc.Add(responseTtl);
                _entries[key] = Entry.CompletedEntry(
                    requestHash,
                    statusCode,
                    body,
                    location,
                    expiresAtUtc);
                _expirations.Enqueue(new Expiration(key, expiresAtUtc), expiresAtUtc);
                CompactExpirationQueueIfNeeded();
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task ReleaseAsync(
        string key,
        string requestHash,
        string ownerToken,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot) {
            if (_entries.TryGetValue(key, out Entry? entry) &&
                !entry.Completed &&
                string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal) &&
                string.Equals(entry.OwnerToken, ownerToken, StringComparison.Ordinal)) {
                _entries.Remove(key);
                CompactExpirationQueueIfNeeded();
            }
        }

        return Task.CompletedTask;
    }

    private void RemoveExpiredEntries(DateTime nowUtc) {
        while (_expirations.TryPeek(out Expiration? expiration, out DateTime expiresAtUtc) && expiresAtUtc <= nowUtc) {
            _expirations.Dequeue();
            if (expiration is not null &&
                _entries.TryGetValue(expiration.Key, out Entry? entry) &&
                entry.ExpiresAtUtc == expiration.ExpiresAtUtc) {
                _entries.Remove(expiration.Key);
            }
        }

        CompactExpirationQueueIfNeeded();
    }

    private void CompactExpirationQueueIfNeeded() {
        if (_expirations.Count <= _maximumEntries + _entries.Count) {
            return;
        }

        _expirations.Clear();
        foreach ((string key, Entry entry) in _entries) {
            _expirations.Enqueue(new Expiration(key, entry.ExpiresAtUtc), entry.ExpiresAtUtc);
        }
    }

    private sealed record Expiration(string Key, DateTime ExpiresAtUtc);

    private sealed record Entry(
        string RequestHash,
        string? OwnerToken,
        bool Completed,
        int? StatusCode,
        string? Body,
        string? Location,
        DateTime ExpiresAtUtc) {
        public static Entry InProgress(string requestHash, string ownerToken, DateTime expiresAtUtc) =>
            new(requestHash, ownerToken, Completed: false, StatusCode: null, Body: null, Location: null, expiresAtUtc);

        public static Entry CompletedEntry(
            string requestHash,
            int statusCode,
            string? body,
            string? location,
            DateTime expiresAtUtc) =>
            new(requestHash, OwnerToken: null, Completed: true, statusCode, body, location, expiresAtUtc);
    }
}
