using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface IAdminSsoService {
    Task<AdminSsoCode> CreateCodeAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<UserId?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
}

public sealed record AdminSsoCode(string Code, DateTime ExpiresAtUtc);
