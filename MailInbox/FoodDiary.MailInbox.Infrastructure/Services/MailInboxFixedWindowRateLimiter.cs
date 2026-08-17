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
        string key = HashKey(scope, value);
        DateTimeOffset now = timeProvider.GetUtcNow();
        lock (_gate) {
            if (_windows.TryGetValue(key, out WindowCounter? counter)) {
                if (now - counter.StartedAtUtc >= window) {
                    _windows[key] = new WindowCounter(now, Count: 1);
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
                return false;
            }

            _windows.Add(key, new WindowCounter(now, Count: 1));
            return true;
        }
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

    private sealed record WindowCounter(DateTimeOffset StartedAtUtc, int Count);
}
