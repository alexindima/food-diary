namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public interface IAdminSsoService {
    Task<AdminSsoCode> CreateCodeAsync(AdminSsoIssuer issuer, CancellationToken cancellationToken = default);
    Task<AdminSsoIssuer?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
}
