using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;

public interface IAuthenticationSessionLifetimePolicy {
    TimeSpan GetLifetime(AuthenticationCredentialKind kind, bool rememberMe);
    DateTime? GetRenewedExpiration(DateTime createdAtUtc, DateTime expiresAtUtc, bool rememberMe, DateTime nowUtc);
    DateTime? GetAbsoluteExpiration(DateTime createdAtUtc);
}
