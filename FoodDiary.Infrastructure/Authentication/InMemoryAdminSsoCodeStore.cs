using System.Collections.Concurrent;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;

namespace FoodDiary.Infrastructure.Authentication;

internal sealed class InMemoryAdminSsoCodeStore(TimeProvider timeProvider) : IAdminSsoCodeStore {
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public Task StoreAsync(
        string code,
        string userId,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        _entries[code] = new Entry(userId, timeProvider.GetUtcNow().Add(lifetime));
        return Task.CompletedTask;
    }

    public Task<string?> ConsumeAsync(string code, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_entries.TryRemove(code, out Entry? entry) || entry.ExpiresAtUtc <= timeProvider.GetUtcNow()) {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(entry.UserId);
    }

    private sealed record Entry(string UserId, DateTimeOffset ExpiresAtUtc);
}
