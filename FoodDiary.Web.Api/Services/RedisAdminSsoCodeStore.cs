using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using StackExchange.Redis;

namespace FoodDiary.Web.Api.Services;

public sealed class RedisAdminSsoCodeStore(IConnectionMultiplexer connectionMultiplexer) : IAdminSsoCodeStore {
    private const string KeyPrefix = "fooddiary:admin-sso:";

    public async Task StoreAsync(
        string code,
        string userId,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        await connectionMultiplexer
            .GetDatabase()
            .StringSetAsync(BuildKey(code), userId, lifetime)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<string?> ConsumeAsync(string code, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        RedisValue value = await connectionMultiplexer
            .GetDatabase()
            .StringGetDeleteAsync(BuildKey(code))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    private static RedisKey BuildKey(string code) => KeyPrefix + code;
}
