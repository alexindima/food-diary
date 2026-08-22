using System.Text.Json;
using FoodDiary.Presentation.Api.Filters;
using StackExchange.Redis;

namespace FoodDiary.Web.Api.Services;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class RedisIdempotencyStore(IConnectionMultiplexer connectionMultiplexer) : IIdempotencyStore {
    private const string KeyPrefix = "fooddiary:idempotency:";
    private const int CompletedState = 1;
    private const int ActiveLockState = 2;
    private const int AcquiredState = 3;
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
        string ownerToken = Guid.NewGuid().ToString("N");
        string lockValue = BuildLockValue(requestHash, ownerToken);
        const string script = """
            local response = redis.call('GET', KEYS[1])
            if response then return {1, response} end

            local activeLock = redis.call('GET', KEYS[2])
            if activeLock then return {2, activeLock} end

            redis.call('SET', KEYS[2], ARGV[1], 'PX', ARGV[2])
            return {3, ''}
            """;

        for (int attempt = 0; attempt < 2; attempt++) {
            cancellationToken.ThrowIfCancellationRequested();
            RedisResult result = await database.ScriptEvaluateAsync(
                script,
                [responseKey, lockKey],
                [
                    lockValue,
                    Math.Max(1L, (long)processingTtl.TotalMilliseconds)
                        .ToString(System.Globalization.CultureInfo.InvariantCulture),
                ]).WaitAsync(cancellationToken).ConfigureAwait(false);
            IdempotencyReservation? reservation = ParseReservation(
                result,
                requestHash,
                ownerToken,
                out string? corruptResponse);
            if (reservation is not null) {
                return reservation;
            }

            await DeleteCorruptResponseAsync(
                database,
                responseKey,
                corruptResponse!,
                cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("The cached idempotency response could not be read.");
    }

    private static IdempotencyReservation? ParseReservation(
        RedisResult result,
        string requestHash,
        string ownerToken,
        out string? corruptResponse) {
        var values = (RedisResult[])result!;
        int state = (int)values[0];
        string storedValue = values[1].ToString();
        corruptResponse = null;

        if (state == AcquiredState) {
            return new IdempotencyReservation(IdempotencyReservationStatus.Acquired, OwnerToken: ownerToken);
        }

        if (state == ActiveLockState) {
            return HasRequestHash(storedValue, requestHash)
                ? new IdempotencyReservation(IdempotencyReservationStatus.InProgress)
                : new IdempotencyReservation(IdempotencyReservationStatus.Conflict);
        }

        if (state != CompletedState) {
            throw new InvalidOperationException(
                $"Unexpected Redis idempotency reservation state: {state.ToString(System.Globalization.CultureInfo.InvariantCulture)}.");
        }

        CompletedEntry? entry = TryDeserialize(storedValue);
        if (entry is null) {
            corruptResponse = storedValue;
            return null;
        }

        if (!string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal)) {
            return new IdempotencyReservation(IdempotencyReservationStatus.Conflict);
        }

        return new IdempotencyReservation(
            IdempotencyReservationStatus.Replay,
            entry.StatusCode,
            entry.Body,
            entry.Location);
    }

    private static async Task DeleteCorruptResponseAsync(
        IDatabase database,
        RedisKey responseKey,
        string corruptResponse,
        CancellationToken cancellationToken) {
        const string script = """
            if redis.call('GET', KEYS[1]) ~= ARGV[1] then return 0 end
            return redis.call('DEL', KEYS[1])
            """;
        await database.ScriptEvaluateAsync(
            script,
            [responseKey],
            [corruptResponse]).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CompleteAsync(
        string key,
        string requestHash,
        string ownerToken,
        int statusCode,
        string? body,
        string? location,
        TimeSpan responseTtl,
        CancellationToken cancellationToken = default) {
        IDatabase database = connectionMultiplexer.GetDatabase();
        var entry = new CompletedEntry(requestHash, statusCode, body, location);

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
            ]).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(
        string key,
        string requestHash,
        string ownerToken,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        const string script = """
            if redis.call('GET', KEYS[1]) ~= ARGV[1] then return 0 end
            return redis.call('DEL', KEYS[1])
            """;
        await connectionMultiplexer.GetDatabase().ScriptEvaluateAsync(
            script,
            [BuildLockKey(key)],
            [BuildLockValue(requestHash, ownerToken)]).WaitAsync(cancellationToken).ConfigureAwait(false);
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

    private sealed record CompletedEntry(string RequestHash, int StatusCode, string? Body, string? Location);
}
