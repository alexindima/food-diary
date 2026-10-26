using FoodDiary.Authentication.Contracts.Options;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Authentication;

public sealed class AuthenticationSessionLifetimePolicy(IOptions<JwtOptions> options, IOptions<BrowserSessionRenewalOptions> renewal) : IAuthenticationSessionLifetimePolicy {
    public TimeSpan GetLifetime(AuthenticationCredentialKind kind, bool rememberMe) => kind switch {
        AuthenticationCredentialKind.AdminBrowser => TimeSpan.FromMinutes(options.Value.ExpirationMinutes),
        AuthenticationCredentialKind.Browser => GetBrowserLifetime(rememberMe),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public DateTime? GetAbsoluteExpiration(DateTime createdAtUtc) => renewal.Value.AbsoluteLifetimeDays is { } days
        ? createdAtUtc.AddDays(days) : null;

    private TimeSpan GetBrowserLifetime(bool rememberMe) {
        var lifetime = TimeSpan.FromDays(rememberMe ? options.Value.RememberMeRefreshTokenExpirationDays : options.Value.RefreshTokenExpirationDays);
        return renewal.Value.AbsoluteLifetimeDays is { } days && TimeSpan.FromDays(days) < lifetime ? TimeSpan.FromDays(days) : lifetime;
    }

    public DateTime? GetRenewedExpiration(DateTime createdAtUtc, DateTime expiresAtUtc, bool rememberMe, DateTime nowUtc) {
        BrowserSessionRenewalOptions settings = renewal.Value;
        if (expiresAtUtc <= nowUtc || expiresAtUtc - nowUtc > TimeSpan.FromDays(settings.RenewalWindowDays)) {
            return null;
        }
        DateTime expires = nowUtc.Add(GetLifetime(AuthenticationCredentialKind.Browser, rememberMe));
        if (settings.AbsoluteLifetimeDays is { } days) {
            DateTime maximum = createdAtUtc.AddDays(days);
            if (maximum < expires) {
                expires = maximum;
            }
        }
        return expires > expiresAtUtc ? expires : null;
    }
}
