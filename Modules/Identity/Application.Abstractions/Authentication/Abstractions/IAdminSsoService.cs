using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public interface IAdminSsoService {
    Task<AdminSsoCode> CreateCodeAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<UserId?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
}
