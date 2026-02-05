using System.Security.Cryptography;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Distributed;

namespace FoodDiary.Infrastructure.Authentication;

public sealed class AdminSsoService(IDistributedCache cache) : IAdminSsoService
{
    private const string CachePrefix = "admin-sso:";
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(2);

    public async Task<AdminSsoCode> CreateCodeAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var code = GenerateCode();
        var expiresAt = DateTime.UtcNow.Add(CodeTtl);
        var key = CachePrefix + code;

        await cache.SetStringAsync(
            key,
            userId.Value.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt },
            cancellationToken);

        return new AdminSsoCode(code, expiresAt);
    }

    public async Task<UserId?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var key = CachePrefix + code;
        var value = await cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        await cache.RemoveAsync(key, cancellationToken);

        return Guid.TryParse(value, out var id) ? new UserId(id) : null;
    }

    private static string GenerateCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var text = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return text;
    }
}
