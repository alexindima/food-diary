using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Authentication;

public sealed class InMemoryPasswordLoginThrottle(
    IOptions<PasswordLoginThrottleOptions> options,
    TimeProvider timeProvider) : IPasswordLoginThrottle {
    private readonly PasswordLoginThrottleOptions _settings = options.Value;
    private readonly Dictionary<string, AttemptState> _states = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();
    private DateTimeOffset _nextCleanup;

    public Task<PasswordLoginAttempt> TryBeginAsync(string email, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        string key = PasswordLoginThrottleKey.FromEmail(email);
        lock (_gate) {
            DateTimeOffset now = timeProvider.GetUtcNow();
            if (now >= _nextCleanup) {
                foreach (string expiredKey in _states.Where(pair => pair.Value.IsExpired(now)).Select(pair => pair.Key).ToArray()) {
                    _states.Remove(expiredKey);
                }
                _nextCleanup = now.AddMinutes(1);
            }

            if (!_states.TryGetValue(key, out AttemptState? state)) {
                if (_states.Count >= _settings.MaximumTrackedEmails) {
                    return Task.FromResult(new PasswordLoginAttempt(key, OwnerToken: null, RetryAfterSeconds: 1));
                }
                state = new AttemptState();
                _states.Add(key, state);
            }

            if (state.ExpiresAt <= now) {
                state.Failures = 0;
                state.BlockedUntil = default;
            }
            if (state.BlockedUntil > now) {
                return Task.FromResult(new PasswordLoginAttempt(key, OwnerToken: null,
                    RetryAfterSeconds: Math.Max(1, (int)Math.Ceiling((state.BlockedUntil - now).TotalSeconds))));
            }
            if (state.LeaseUntil > now) {
                return Task.FromResult(new PasswordLoginAttempt(key, OwnerToken: null, RetryAfterSeconds: 1));
            }

            state.OwnerToken = Guid.NewGuid().ToString("N");
            state.LeaseUntil = now.AddSeconds(_settings.AttemptLeaseSeconds);
            return Task.FromResult(new PasswordLoginAttempt(key, state.OwnerToken));
        }
    }

    public Task<bool> CompleteAsync(
        PasswordLoginAttempt attempt,
        PasswordLoginAttemptOutcome outcome,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) {
            DateTimeOffset now = timeProvider.GetUtcNow();
            if (!attempt.IsAcquired || !_states.TryGetValue(attempt.Key, out AttemptState? state) ||
                !string.Equals(state.OwnerToken, attempt.OwnerToken, StringComparison.Ordinal) || state.LeaseUntil <= now) {
                return Task.FromResult(false);
            }

            state.OwnerToken = null;
            state.LeaseUntil = default;
            if (outcome == PasswordLoginAttemptOutcome.Succeeded) {
                _states.Remove(attempt.Key);
            } else if (outcome == PasswordLoginAttemptOutcome.InvalidCredentials) {
                state.Failures = state.ExpiresAt <= now ? 1 : Math.Min(state.Failures + 1, _settings.FailureThreshold + 20);
                state.ExpiresAt = now.AddSeconds(_settings.FailureWindowSeconds);
                if (state.Failures >= _settings.FailureThreshold) {
                    double cooldown = Math.Min(_settings.MaximumCooldownSeconds,
                        _settings.InitialCooldownSeconds * Math.Pow(2, state.Failures - _settings.FailureThreshold));
                    state.BlockedUntil = now.AddSeconds(cooldown);
                }
            } else if (state.IsExpired(now)) {
                _states.Remove(attempt.Key);
            }
            return Task.FromResult(true);
        }
    }

    private sealed class AttemptState {
        public int Failures { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset BlockedUntil { get; set; }
        public string? OwnerToken { get; set; }
        public DateTimeOffset LeaseUntil { get; set; }

        public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now && LeaseUntil <= now;
    }
}
