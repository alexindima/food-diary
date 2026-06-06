using System.Security.Cryptography;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Caching.Distributed;

namespace FoodDiary.Infrastructure.Authentication;

public sealed class AdminSsoService(IDistributedCache cache, TimeProvider dateTimeProvider) : IAdminSsoService {
    private const string CachePrefix = "admin-sso:";
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(2);

    public async Task<AdminSsoCode> CreateCodeAsync(UserId userId, CancellationToken cancellationToken = default) {
        string code = GenerateCode();
        DateTime expiresAt = dateTimeProvider.GetUtcNow().UtcDateTime.Add(CodeTtl);
        string key = CachePrefix + code;

        await cache.SetStringAsync(
            key,
            userId.Value.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpiration = new DateTimeOffset(expiresAt, TimeSpan.Zero) },
            cancellationToken).ConfigureAwait(false);

        return new AdminSsoCode(code, expiresAt);
    }

    public async Task<UserId?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(code)) {
            return null;
        }

        string key = CachePrefix + code;
        string? value = await cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);

        return Guid.TryParse(value, out Guid id) ? new UserId(id) : null;
    }

    private static string GenerateCode() {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
