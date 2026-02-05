using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Authentication;

public interface IAdminSsoService
{
    Task<AdminSsoCode> CreateCodeAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<UserId?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
}

public sealed record AdminSsoCode(string Code, DateTime ExpiresAtUtc);
