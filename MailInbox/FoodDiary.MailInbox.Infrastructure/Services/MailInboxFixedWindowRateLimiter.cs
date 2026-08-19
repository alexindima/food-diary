using System.Security.Cryptography;
using System.Text;
using FoodDiary.MailInbox.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class MailInboxFixedWindowRateLimiter(
    IOptions<MailInboxSmtpOptions> options,
    TimeProvider timeProvider,
    byte[]? overflowHashKey = null) {
    private const int OverflowShardCount = 4096;
    private readonly Lock _gate = new();
    private readonly byte[] _overflowHashKey = CreateOverflowHashKey(overflowHashKey);
    private readonly ScopeState _ipState = new(GetIpTrackedCapacity(options.Value.MaxTrackedRateLimitKeys));
    private readonly ScopeState _ipByteState = new(GetIpByteTrackedCapacity(options.Value.MaxTrackedRateLimitKeys));
    private readonly ScopeState _senderState = new(GetSenderTrackedCapacity(options.Value.MaxTrackedRateLimitKeys));
    private readonly ScopeState _fallbackState = new(trackedCapacity: 0);

    public bool TryAcquire(
        string scope,
        string value,
        long permitLimit,
        TimeSpan window,
        long permits = 1) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permits);
        if (permits > permitLimit) {
            return false;
        }

        string normalizedScope = scope.Trim().ToLowerInvariant();
        string key = HashKey(normalizedScope, value);
        DateTimeOffset now = timeProvider.GetUtcNow();
        lock (_gate) {
            ScopeState state = GetScopeState(normalizedScope);
            RemoveExpiredWindows(state, now);
            if (state.Windows.TryGetValue(key, out WindowCounter? counter)) {
                if (counter.Count > permitLimit - permits) {
                    return false;
                }

                state.Windows[key] = counter with { Count = counter.Count + permits };
                return true;
            }

            if (state.Windows.Count >= state.TrackedCapacity) {
                return TryAcquireOverflow(state, key, permitLimit, window, now, permits);
            }

            AddWindow(state, key, now + window, permits);
            return true;
        }
    }

    private ScopeState GetScopeState(string scope) => scope switch {
        "ip" => _ipState,
        "ip-bytes" => _ipByteState,
        "sender" => _senderState,
        _ => _fallbackState,
    };

    private bool TryAcquireOverflow(
        ScopeState state,
        string key,
        long permitLimit,
        TimeSpan window,
        DateTimeOffset now,
        long permits) {
        int shard = GetOverflowShard(key);
        WindowCounter? counter = state.OverflowWindows[shard];
        if (counter is null || counter.ExpiresAtUtc <= now) {
            state.OverflowWindows[shard] = new WindowCounter(now + window, permits);
            return true;
        }

        if (counter.Count > permitLimit - permits) {
            return false;
        }

        state.OverflowWindows[shard] = counter with { Count = counter.Count + permits };
        return true;
    }

    private static void AddWindow(
        ScopeState state,
        string key,
        DateTimeOffset expiresAtUtc,
        long permits) {
        state.Windows.Add(key, new WindowCounter(expiresAtUtc, permits));
        state.Expirations.Enqueue(
            new WindowExpiration(key, expiresAtUtc),
            expiresAtUtc.UtcDateTime.Ticks);
    }

    private static void RemoveExpiredWindows(ScopeState state, DateTimeOffset now) {
        while (state.Expirations.TryPeek(out WindowExpiration? expiration, out long expiresAtTicks) &&
               expiresAtTicks <= now.UtcDateTime.Ticks) {
            state.Expirations.Dequeue();
            if (state.Windows.TryGetValue(expiration.Key, out WindowCounter? counter) &&
                counter.ExpiresAtUtc == expiration.ExpiresAtUtc) {
                state.Windows.Remove(expiration.Key);
            }
        }
    }

    private int GetOverflowShard(string key) {
        byte[] hash = HMACSHA256.HashData(_overflowHashKey, Encoding.ASCII.GetBytes(key));
        return (hash[0] << 4) | (hash[1] >> 4);
    }

    private static string HashKey(string scope, string value) {
        byte[] bytes = Encoding.UTF8.GetBytes(string.Concat(scope, "\n", value.Trim().ToLowerInvariant()));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static byte[] CreateOverflowHashKey(byte[]? configuredKey) {
        if (configuredKey is null) {
            return RandomNumberGenerator.GetBytes(32);
        }

        if (configuredKey.Length < 16) {
            throw new ArgumentException("Overflow hash key must contain at least 128 bits.", nameof(configuredKey));
        }

        return [.. configuredKey];
    }

    private static int GetSenderTrackedCapacity(int totalCapacity) => totalCapacity / 2;

    private static int GetIpTrackedCapacity(int totalCapacity) =>
        ((totalCapacity - GetSenderTrackedCapacity(totalCapacity)) + 1) / 2;

    private static int GetIpByteTrackedCapacity(int totalCapacity) =>
        (totalCapacity - GetSenderTrackedCapacity(totalCapacity)) / 2;

    private sealed class ScopeState(int trackedCapacity) {
        public int TrackedCapacity { get; } = trackedCapacity;

        public Dictionary<string, WindowCounter> Windows { get; } = new(StringComparer.Ordinal);

        public PriorityQueue<WindowExpiration, long> Expirations { get; } = new();

        public WindowCounter?[] OverflowWindows { get; } = new WindowCounter?[OverflowShardCount];
    }

    private sealed record WindowCounter(DateTimeOffset ExpiresAtUtc, long Count);

    private sealed record WindowExpiration(string Key, DateTimeOffset ExpiresAtUtc);
}
