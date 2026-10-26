using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FoodDiary.Modules.Identity.Infrastructure.Authentication;

public sealed class RedisPasswordLoginThrottle(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<PasswordLoginThrottleOptions> options) : IPasswordLoginThrottle {
    private const string KeyPrefix = "fooddiary:password-login:";

    public async Task<PasswordLoginAttempt> TryBeginAsync(string email, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        string key = PasswordLoginThrottleKey.FromEmail(email);
        string ownerToken = Guid.NewGuid().ToString("N");
        const string script = """
            local time = redis.call('TIME')
            local now = tonumber(time[1]) * 1000 + math.floor(tonumber(time[2]) / 1000)
            local state = redis.call('HMGET', KEYS[1], 'expires', 'blocked', 'lease')
            local expires = tonumber(state[1]) or 0
            local blocked = tonumber(state[2]) or 0
            local lease = tonumber(state[3]) or 0
            if expires <= now then
                redis.call('HDEL', KEYS[1], 'failures', 'blocked', 'expires')
                expires = 0
                blocked = 0
            end
            if blocked > now then return math.ceil((blocked - now) / 1000) end
            if lease > now then return 1 end
            local untilTime = now + tonumber(ARGV[2])
            redis.call('HSET', KEYS[1], 'owner', ARGV[1], 'lease', untilTime)
            redis.call('PEXPIRE', KEYS[1], math.max(expires, untilTime) - now)
            return 0
            """;
        RedisResult result = await connectionMultiplexer.GetDatabase().ScriptEvaluateAsync(
            script, [KeyPrefix + key], [ownerToken, options.Value.AttemptLeaseSeconds * 1000L])
            .WaitAsync(cancellationToken).ConfigureAwait(false);
        int retryAfter = (int)result;
        return new PasswordLoginAttempt(key, retryAfter == 0 ? ownerToken : null, retryAfter);
    }

    public async Task<bool> CompleteAsync(
        PasswordLoginAttempt attempt,
        PasswordLoginAttemptOutcome outcome,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!attempt.IsAcquired) {
            return false;
        }
        const string script = """
            local time = redis.call('TIME')
            local now = tonumber(time[1]) * 1000 + math.floor(tonumber(time[2]) / 1000)
            local state = redis.call('HMGET', KEYS[1], 'owner', 'lease', 'expires', 'failures')
            if state[1] ~= ARGV[1] or (tonumber(state[2]) or 0) <= now then return 0 end
            if ARGV[2] == 'Succeeded' then
                redis.call('DEL', KEYS[1])
                return 1
            end
            redis.call('HDEL', KEYS[1], 'owner', 'lease')
            local expires = tonumber(state[3]) or 0
            if ARGV[2] == 'InvalidCredentials' then
                local failures = expires <= now and 1 or math.min((tonumber(state[4]) or 0) + 1, tonumber(ARGV[3]) + 20)
                expires = now + tonumber(ARGV[4])
                redis.call('HSET', KEYS[1], 'failures', failures, 'expires', expires)
                if failures >= tonumber(ARGV[3]) then
                    local cooldown = math.min(tonumber(ARGV[6]), tonumber(ARGV[5]) * 2 ^ (failures - tonumber(ARGV[3])))
                    redis.call('HSET', KEYS[1], 'blocked', now + cooldown)
                end
            end
            if expires <= now then redis.call('DEL', KEYS[1])
            else redis.call('PEXPIRE', KEYS[1], expires - now) end
            return 1
            """;
        PasswordLoginThrottleOptions settings = options.Value;
        RedisResult result = await connectionMultiplexer.GetDatabase().ScriptEvaluateAsync(script,
            [KeyPrefix + attempt.Key],
            [attempt.OwnerToken, outcome.ToString(), settings.FailureThreshold, settings.FailureWindowSeconds * 1000L,
                settings.InitialCooldownSeconds * 1000L, settings.MaximumCooldownSeconds * 1000L])
            .WaitAsync(cancellationToken).ConfigureAwait(false);
        return (int)result == 1;
    }
}
