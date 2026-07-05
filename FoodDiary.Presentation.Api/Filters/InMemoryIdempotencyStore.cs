namespace FoodDiary.Presentation.Api.Filters;

public sealed class InMemoryIdempotencyStore(TimeProvider timeProvider) : IIdempotencyStore {
    private readonly Lock syncRoot = new();
    private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);

    public Task<IdempotencyReservation> ReserveAsync(
        string key,
        string requestHash,
        TimeSpan responseTtl,
        TimeSpan processingTtl,
        CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        lock (syncRoot) {
            RemoveExpiredEntries(nowUtc);

            if (entries.TryGetValue(key, out Entry? entry)) {
                if (!string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal)) {
                    return Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.Conflict));
                }

                if (entry.Completed) {
                    return Task.FromResult(new IdempotencyReservation(
                        IdempotencyReservationStatus.Replay,
                        entry.StatusCode,
                        entry.Body));
                }

                return Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.InProgress));
            }

            entries[key] = Entry.InProgress(requestHash, nowUtc.Add(processingTtl));
            return Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.Acquired));
        }
    }

    public Task CompleteAsync(
        string key,
        string requestHash,
        int statusCode,
        string? body,
        TimeSpan responseTtl,
        CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        lock (syncRoot) {
            entries[key] = Entry.CompletedEntry(requestHash, statusCode, body, nowUtc.Add(responseTtl));
        }

        return Task.CompletedTask;
    }

    private void RemoveExpiredEntries(DateTime nowUtc) {
        string[] expiredKeys = [.. entries
            .Where(entry => entry.Value.ExpiresAtUtc <= nowUtc)
            .Select(static entry => entry.Key)];

        foreach (string key in expiredKeys) {
            entries.Remove(key);
        }
    }

    private sealed record Entry(
        string RequestHash,
        bool Completed,
        int? StatusCode,
        string? Body,
        DateTime ExpiresAtUtc) {
        public static Entry InProgress(string requestHash, DateTime expiresAtUtc) =>
            new(requestHash, Completed: false, StatusCode: null, Body: null, expiresAtUtc);

        public static Entry CompletedEntry(string requestHash, int statusCode, string? body, DateTime expiresAtUtc) =>
            new(requestHash, Completed: true, statusCode, body, expiresAtUtc);
    }
}
