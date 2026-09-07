using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using FoodDiary.MailInbox.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class MailInboxSlidingWindowRateLimiter(
    IOptions<MailInboxSmtpOptions> options,
    TimeProvider timeProvider,
    byte[]? overflowHashKey = null) {
    private const int OverflowShardCount = 4096;
    private const int AbsoluteMaxEntriesPerCounter = 1024;
    private readonly Lock _gate = new();
    private readonly byte[] _overflowHashKey = CreateOverflowHashKey(overflowHashKey);
    private readonly ScopeState _ipState = new(
        GetIpTrackedCapacity(options.Value.MaxTrackedRateLimitKeys),
        GetMaxEntries(options.Value.MaxMessagesPerIpPerHour));
    private readonly ScopeState _ipByteState = new(
        GetIpByteTrackedCapacity(options.Value.MaxTrackedRateLimitKeys),
        GetMaxEntries(options.Value.MaxMessagesPerIpPerHour));
    private readonly ScopeState _senderState = new(
        GetSenderTrackedCapacity(options.Value.MaxTrackedRateLimitKeys),
        GetMaxEntries(options.Value.MaxMessagesPerSenderPerHour));
    private readonly ScopeState _fallbackState = new(trackedCapacity: 0, maxEntriesPerCounter: 128);

    public bool TryAcquire(
        string scope,
        string value,
        long permitLimit,
        TimeSpan window,
        long permits = 1) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permits);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);
        if (permits > permitLimit) {
            return false;
        }

        string normalizedScope = scope.Trim().ToLowerInvariant();
        string key = HashKey(normalizedScope, value);
        long nowTicks = timeProvider.GetUtcNow().UtcDateTime.Ticks;
        lock (_gate) {
            ScopeState state = GetScopeState(normalizedScope);
            RemoveExpiredCounters(state, nowTicks);
            if (state.Counters.TryGetValue(key, out SlidingWindowCounter? counter)) {
                ResetForWindowChange(counter, window.Ticks);
                RemoveExpiredEntries(counter, nowTicks - window.Ticks);
                if (!TryCharge(counter, permitLimit, permits, nowTicks)) {
                    return false;
                }

                EnsureExpirationScheduled(state, key, counter);
                return true;
            }

            if (state.Counters.Count >= state.TrackedCapacity) {
                return TryAcquireOverflow(state, key, permitLimit, window, nowTicks, permits);
            }

            var newCounter = new SlidingWindowCounter(window.Ticks, state.MaxEntriesPerCounter);
            _ = TryCharge(newCounter, permitLimit, permits, nowTicks);
            state.Counters.Add(key, newCounter);
            EnsureExpirationScheduled(state, key, newCounter);
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
        long nowTicks,
        long permits) {
        int shard = GetOverflowShard(key);
        SlidingWindowCounter? counter = state.OverflowCounters[shard];
        if (counter is null) {
            counter = new SlidingWindowCounter(window.Ticks, state.MaxEntriesPerCounter);
            state.OverflowCounters[shard] = counter;
        } else {
            ResetForWindowChange(counter, window.Ticks);
            RemoveExpiredEntries(counter, nowTicks - window.Ticks);
        }

        return TryCharge(counter, permitLimit, permits, nowTicks);
    }

    private static bool TryCharge(
        SlidingWindowCounter counter,
        long permitLimit,
        long permits,
        long nowTicks) {
        if (counter.PermitCount > permitLimit - permits) {
            return false;
        }

        if (counter.Entries.Count < counter.MaxEntries && counter.CoalescedTailPermits == 0) {
            counter.Entries.Enqueue(new SlidingWindowEntry(nowTicks, permits));
        } else {
            counter.CoalescedTailTimestampTicks = nowTicks;
            counter.CoalescedTailPermits += permits;
        }

        counter.PermitCount += permits;
        return true;
    }

    private static void ResetForWindowChange(SlidingWindowCounter counter, long windowTicks) {
        if (counter.WindowTicks == windowTicks) {
            return;
        }

        counter.Entries.Clear();
        counter.CoalescedTailPermits = 0;
        counter.CoalescedTailTimestampTicks = 0;
        counter.PermitCount = 0;
        counter.WindowTicks = windowTicks;
        counter.ExpirationScheduled = false;
    }

    private static void RemoveExpiredEntries(SlidingWindowCounter counter, long cutoffTicks) {
        while (counter.Entries.TryPeek(out SlidingWindowEntry entry) && entry.TimestampTicks <= cutoffTicks) {
            _ = counter.Entries.Dequeue();
            counter.PermitCount -= entry.Permits;
        }

        if (counter.Entries.Count == 0 &&
            counter.CoalescedTailPermits > 0 &&
            counter.CoalescedTailTimestampTicks <= cutoffTicks) {
            counter.PermitCount -= counter.CoalescedTailPermits;
            counter.CoalescedTailPermits = 0;
            counter.CoalescedTailTimestampTicks = 0;
        }
    }

    private static void EnsureExpirationScheduled(
        ScopeState state,
        string key,
        SlidingWindowCounter counter) {
        if (counter.ExpirationScheduled || !TryGetEarliestTimestamp(counter, out long earliestTimestampTicks)) {
            return;
        }

        long expiresAtTicks = earliestTimestampTicks + counter.WindowTicks;
        counter.ExpirationScheduled = true;
        counter.ScheduledExpirationTicks = expiresAtTicks;
        state.Expirations.Enqueue(new CounterExpiration(key, expiresAtTicks), expiresAtTicks);
    }

    private static void RemoveExpiredCounters(ScopeState state, long nowTicks) {
        while (state.Expirations.TryPeek(out CounterExpiration? expiration, out long expiresAtTicks) &&
               expiresAtTicks <= nowTicks) {
            _ = state.Expirations.Dequeue();
            if (!state.Counters.TryGetValue(expiration.Key, out SlidingWindowCounter? counter) ||
                counter.ScheduledExpirationTicks != expiration.ExpiresAtTicks) {
                continue;
            }

            counter.ExpirationScheduled = false;
            RemoveExpiredEntries(counter, nowTicks - counter.WindowTicks);
            if (counter.Entries.Count == 0 && counter.CoalescedTailPermits == 0) {
                _ = state.Counters.Remove(expiration.Key);
                continue;
            }

            EnsureExpirationScheduled(state, expiration.Key, counter);
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

    private static int GetMaxEntries(int configuredLimit) =>
        Math.Min(configuredLimit, AbsoluteMaxEntriesPerCounter);

    private static bool TryGetEarliestTimestamp(
        SlidingWindowCounter counter,
        out long timestampTicks) {
        if (counter.Entries.TryPeek(out SlidingWindowEntry entry)) {
            timestampTicks = entry.TimestampTicks;
            return true;
        }

        timestampTicks = counter.CoalescedTailTimestampTicks;
        return counter.CoalescedTailPermits > 0;
    }

    private sealed class ScopeState(int trackedCapacity, int maxEntriesPerCounter) {
        public int TrackedCapacity { get; } = trackedCapacity;

        public int MaxEntriesPerCounter { get; } = maxEntriesPerCounter;

        public Dictionary<string, SlidingWindowCounter> Counters { get; } = new(StringComparer.Ordinal);

        public PriorityQueue<CounterExpiration, long> Expirations { get; } = new();

        public SlidingWindowCounter?[] OverflowCounters { get; } = new SlidingWindowCounter?[OverflowShardCount];
    }

    private sealed class SlidingWindowCounter(long windowTicks, int maxEntries) {
        public Queue<SlidingWindowEntry> Entries { get; } = new();

        public int MaxEntries { get; } = maxEntries;

        public long CoalescedTailTimestampTicks { get; set; }

        public long CoalescedTailPermits { get; set; }

        public long PermitCount { get; set; }

        public long WindowTicks { get; set; } = windowTicks;

        public bool ExpirationScheduled { get; set; }

        public long ScheduledExpirationTicks { get; set; }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct SlidingWindowEntry(long TimestampTicks, long Permits);

    private sealed record CounterExpiration(string Key, long ExpiresAtTicks);
}
