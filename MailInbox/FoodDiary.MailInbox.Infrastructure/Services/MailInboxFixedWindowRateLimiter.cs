using System.Security.Cryptography;
using System.Text;
using FoodDiary.MailInbox.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class MailInboxFixedWindowRateLimiter(
    IOptions<MailInboxSmtpOptions> options,
    TimeProvider timeProvider) {
    private readonly Lock _gate = new();
    private readonly Dictionary<string, WindowCounter> _windows = new(StringComparer.Ordinal);
    private readonly int _maxTrackedKeys = options.Value.MaxTrackedRateLimitKeys;

    public bool TryAcquire(string scope, string value, int permitLimit, TimeSpan window) {
        string normalizedScope = scope.Trim().ToLowerInvariant();
        string key = HashKey(normalizedScope, value);
        DateTimeOffset now = timeProvider.GetUtcNow();
        lock (_gate) {
            if (_windows.TryGetValue(key, out WindowCounter? counter)) {
                if (now - counter.StartedAtUtc >= window) {
                    _windows[key] = new WindowCounter(normalizedScope, now, Count: 1);
                    return true;
                }

                if (counter.Count >= permitLimit) {
                    return false;
                }

                _windows[key] = counter with { Count = counter.Count + 1 };
                return true;
            }

            RemoveExpiredWindows(now, window);
            if (_windows.Count >= _maxTrackedKeys) {
                EvictOldestWindow(normalizedScope);
            }

            _windows.Add(key, new WindowCounter(normalizedScope, now, Count: 1));
            return true;
        }
    }

    private void EvictOldestWindow(string scope) {
        KeyValuePair<string, WindowCounter>? oldestInScope = _windows
            .Where(pair => pair.Value.Scope.Equals(scope, StringComparison.Ordinal))
            .OrderBy(static pair => pair.Value.StartedAtUtc)
            .Select(static pair => (KeyValuePair<string, WindowCounter>?)pair)
            .FirstOrDefault();
        KeyValuePair<string, WindowCounter> oldest = oldestInScope ??
            _windows.MinBy(static pair => pair.Value.StartedAtUtc);
        _windows.Remove(oldest.Key);
    }

    private void RemoveExpiredWindows(DateTimeOffset now, TimeSpan currentWindow) {
        DateTimeOffset oldestAllowed = now - currentWindow;
        foreach (string key in _windows
                     .Where(pair => pair.Value.StartedAtUtc <= oldestAllowed)
                     .Select(static pair => pair.Key)
                     .ToArray()) {
            _windows.Remove(key);
        }
    }

    private static string HashKey(string scope, string value) {
        byte[] bytes = Encoding.UTF8.GetBytes(string.Concat(scope, "\n", value.Trim().ToLowerInvariant()));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private sealed record WindowCounter(string Scope, DateTimeOffset StartedAtUtc, int Count);
}
