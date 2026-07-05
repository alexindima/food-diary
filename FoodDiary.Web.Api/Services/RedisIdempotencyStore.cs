using System.Text.Json;
using FoodDiary.Presentation.Api.Filters;
using StackExchange.Redis;

namespace FoodDiary.Web.Api.Services;

public sealed class RedisIdempotencyStore(IConnectionMultiplexer connectionMultiplexer) : IIdempotencyStore {
    private const string KeyPrefix = "fooddiary:idempotency:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IdempotencyReservation> ReserveAsync(
        string key,
        string requestHash,
        TimeSpan responseTtl,
        TimeSpan processingTtl,
        CancellationToken cancellationToken = default) {
        IDatabase database = connectionMultiplexer.GetDatabase();
        RedisKey responseKey = BuildResponseKey(key);
        RedisKey lockKey = BuildLockKey(key);

        IdempotencyReservation? completed = await TryReadCompletedAsync(
            database,
            responseKey,
            requestHash,
            cancellationToken).ConfigureAwait(false);
        if (completed is not null) {
            return completed;
        }

        if (await database.StringSetAsync(lockKey, requestHash, processingTtl, When.NotExists).ConfigureAwait(false)) {
            return new IdempotencyReservation(IdempotencyReservationStatus.Acquired);
        }

        completed = await TryReadCompletedAsync(database, responseKey, requestHash, cancellationToken).ConfigureAwait(false);
        if (completed is not null) {
            return completed;
        }

        RedisValue activeRequestHash = await database.StringGetAsync(lockKey).ConfigureAwait(false);
        if (activeRequestHash.HasValue &&
            !string.Equals(activeRequestHash.ToString(), requestHash, StringComparison.Ordinal)) {
            return new IdempotencyReservation(IdempotencyReservationStatus.Conflict);
        }

        return activeRequestHash.HasValue
            ? new IdempotencyReservation(IdempotencyReservationStatus.InProgress)
            : await TryAcquireAfterExpiredLockAsync(
                database,
                lockKey,
                requestHash,
                processingTtl,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task CompleteAsync(
        string key,
        string requestHash,
        int statusCode,
        string? body,
        TimeSpan responseTtl,
        CancellationToken cancellationToken = default) {
        IDatabase database = connectionMultiplexer.GetDatabase();
        var entry = new CompletedEntry(requestHash, statusCode, body);

        cancellationToken.ThrowIfCancellationRequested();
        await database.StringSetAsync(
            BuildResponseKey(key),
            JsonSerializer.Serialize(entry, JsonOptions),
            responseTtl).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await database.LockReleaseAsync(BuildLockKey(key), requestHash).ConfigureAwait(false);
    }

    private static async Task<IdempotencyReservation?> TryReadCompletedAsync(
        IDatabase database,
        RedisKey responseKey,
        string requestHash,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue cached = await database.StringGetAsync(responseKey).ConfigureAwait(false);
        if (!cached.HasValue) {
            return null;
        }

        CompletedEntry? entry = TryDeserialize(cached.ToString());
        if (entry is null) {
            await database.KeyDeleteAsync(responseKey).ConfigureAwait(false);
            return null;
        }

        return !string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal)
            ? new IdempotencyReservation(IdempotencyReservationStatus.Conflict)
            : new IdempotencyReservation(IdempotencyReservationStatus.Replay, entry.StatusCode, entry.Body);
    }

    private static async Task<IdempotencyReservation> TryAcquireAfterExpiredLockAsync(
        IDatabase database,
        RedisKey lockKey,
        string requestHash,
        TimeSpan processingTtl,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return await database.StringSetAsync(lockKey, requestHash, processingTtl, When.NotExists).ConfigureAwait(false)
            ? new IdempotencyReservation(IdempotencyReservationStatus.Acquired)
            : new IdempotencyReservation(IdempotencyReservationStatus.InProgress);
    }

    private static CompletedEntry? TryDeserialize(string value) {
        try {
            return JsonSerializer.Deserialize<CompletedEntry>(value, JsonOptions);
        } catch (JsonException) {
            return null;
        }
    }

    private static RedisKey BuildResponseKey(string key) => KeyPrefix + key + ":response";

    private static RedisKey BuildLockKey(string key) => KeyPrefix + key + ":lock";

    private sealed record CompletedEntry(string RequestHash, int StatusCode, string? Body);
}
