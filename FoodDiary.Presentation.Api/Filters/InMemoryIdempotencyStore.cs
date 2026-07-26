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

            string ownerToken = Guid.NewGuid().ToString("N");
            entries[key] = Entry.InProgress(requestHash, ownerToken, nowUtc.Add(processingTtl));
            return Task.FromResult(new IdempotencyReservation(
                IdempotencyReservationStatus.Acquired,
                OwnerToken: ownerToken));
        }
    }

    public Task CompleteAsync(
        string key,
        string requestHash,
        string ownerToken,
        int statusCode,
        string? body,
        TimeSpan responseTtl,
        CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        lock (syncRoot) {
            if (entries.TryGetValue(key, out Entry? entry) &&
                !entry.Completed &&
                string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal) &&
                string.Equals(entry.OwnerToken, ownerToken, StringComparison.Ordinal)) {
                entries[key] = Entry.CompletedEntry(requestHash, statusCode, body, nowUtc.Add(responseTtl));
            }
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
        string? OwnerToken,
        bool Completed,
        int? StatusCode,
        string? Body,
        DateTime ExpiresAtUtc) {
        public static Entry InProgress(string requestHash, string ownerToken, DateTime expiresAtUtc) =>
            new(requestHash, ownerToken, Completed: false, StatusCode: null, Body: null, expiresAtUtc);

        public static Entry CompletedEntry(string requestHash, int statusCode, string? body, DateTime expiresAtUtc) =>
            new(requestHash, OwnerToken: null, Completed: true, statusCode, body, expiresAtUtc);
    }
}
