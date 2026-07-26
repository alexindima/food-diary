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

        string ownerToken = Guid.NewGuid().ToString("N");
        string lockValue = BuildLockValue(requestHash, ownerToken);
        if (await database.StringSetAsync(lockKey, lockValue, processingTtl, When.NotExists).ConfigureAwait(false)) {
            return new IdempotencyReservation(IdempotencyReservationStatus.Acquired, OwnerToken: ownerToken);
        }

        completed = await TryReadCompletedAsync(database, responseKey, requestHash, cancellationToken).ConfigureAwait(false);
        if (completed is not null) {
            return completed;
        }

        RedisValue activeLock = await database.StringGetAsync(lockKey).ConfigureAwait(false);
        if (activeLock.HasValue &&
            !HasRequestHash(activeLock.ToString(), requestHash)) {
            return new IdempotencyReservation(IdempotencyReservationStatus.Conflict);
        }

        return activeLock.HasValue
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
        string ownerToken,
        int statusCode,
        string? body,
        TimeSpan responseTtl,
        CancellationToken cancellationToken = default) {
        IDatabase database = connectionMultiplexer.GetDatabase();
        var entry = new CompletedEntry(requestHash, statusCode, body);

        cancellationToken.ThrowIfCancellationRequested();
        const string script = """
            if redis.call('GET', KEYS[1]) ~= ARGV[1] then return 0 end
            redis.call('SET', KEYS[2], ARGV[2], 'PX', ARGV[3])
            redis.call('DEL', KEYS[1])
            return 1
            """;
        await database.ScriptEvaluateAsync(
            script,
            [BuildLockKey(key), BuildResponseKey(key)],
            [
                BuildLockValue(requestHash, ownerToken),
                JsonSerializer.Serialize(entry, JsonOptions),
                ((long)responseTtl.TotalMilliseconds).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ]).ConfigureAwait(false);
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
        string ownerToken = Guid.NewGuid().ToString("N");
        return await database.StringSetAsync(
            lockKey,
            BuildLockValue(requestHash, ownerToken),
            processingTtl,
            When.NotExists).ConfigureAwait(false)
            ? new IdempotencyReservation(IdempotencyReservationStatus.Acquired, OwnerToken: ownerToken)
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

    private static string BuildLockValue(string requestHash, string ownerToken) => $"{requestHash}:{ownerToken}";

    private static bool HasRequestHash(string lockValue, string requestHash) =>
        string.Equals(lockValue, requestHash, StringComparison.Ordinal) ||
        lockValue.StartsWith($"{requestHash}:", StringComparison.Ordinal);

    private sealed record CompletedEntry(string RequestHash, int StatusCode, string? Body);
}
